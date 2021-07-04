using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EEGDataPreprocessor
{
    internal class Program
    {
        private const string INPUT_FOLDER_PATH = "C:\\Users\\serpi\\Desktop\\repos\\EEGSetParser\\run";
        private const string OUTPUT_FOLDER_PATH = "C:\\Users\\serpi\\Desktop\\repos\\EEGSetParser\\run\\out";
        private const int START_OUTPUT_INDEX = 0;
        private const float EMOTION_SCALE_PER_TIME = 0.95f;

        private static readonly List<string> emotions = new List<string>
        {
            "relax",
            "awe",
            "frustration",
            "joy",
            "anger",
            "happy",
            "sad",
            "love",
            "grief",
            "fear",
            "jealousy",
            "relief",
            "disgust",
            "excite",
            "compassion"
        };

        private static readonly List<string> allowAnnos = new List<string>
        {
            "enter",
            "exit",
            "press1",
            "press"
        };

        private static readonly List<string> channelsList = new List<string>
        {
            "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16", 
            "A17", "A18", "A19", "A20", "A21", "A22", "A23", "A24", "A25", "A26", "A27", "A28", "A29", "A30", "A31", "A32", 
            "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "B10", "B11", "B12", "B13", "B14", "B15", "B16", 
            "B17", "B18", "B19", "B20", "B21", "B22", "B23", "B24", "B25", "B26", "B27", "B28", "B29", "B30", "B31", "B32", 
            "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10", "C11", "C12", "C13", "C14", "C15", "C16", 
            "C17", "C18", "C19", "C20", "C21", "C22", "C23", "C24", "C25", "C26", "C27", "C28", "C29", "C30", "C31", "C32", 
            "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D10", "D11", "D12", "D13", "D14", "D15", "D16", 
            "D17", "D18", "D19", "D20", "D21", "D22", "D23", "D24", "D25", "D26", "D27", "D28", "D29", "D30", "D31", "D32", 
            "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "E10", "E11", "E12", "E13", "E14", "E15", "E16", 
            "E17", "E18", "E19", "E20", "E21", "E22", "E23", "E24", "E25", "E26", "E27", "E28", "E29", "E30", "E31", "E32", 
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "F13", "F14", "F15", "F16", 
            "F17", "F18", "F19", "F20", "F21", "F22", "F23", "F24", "F25", "F26", "F27", "F28", "F29", "F30", "F31", "F32", 
            "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9", "G10", "G11", "G12", "G13", "G14", "G15", "G16", 
            "G17", "G18", "G19", "G20", "G21", "G22", "G23", "G24", "G25", "G26", "G27", "G28", "G29", "G30", "G31", "G32", 
            "H1", "H2", "H3", "H4", "H5", "H6", "H7", "H8", "H9", "H10", "H11", "H12", "H13", "H14", "H15", "H16", 
            "H17", "H18", "H19", "H20", "H21", "H22", "H23", "H24", "H25", "H26", "EXG1", "EXG2", "EXG3", "EXG4", "EXG5", "EXG6", 
        };
        
        public static void Main(string[] args)
        {
            
            int outPutIndex = 0;
            int subOutPutIndex = START_OUTPUT_INDEX;
            string[] files = Directory.GetFiles(INPUT_FOLDER_PATH, "*.csv");

            foreach (string filePath in files)
            {
                FileInfo annoFile = new FileInfo(filePath);
                if (!annoFile.Name.EndsWith("_anno.csv"))
                    continue;

                Console.WriteLine("Start processing " + annoFile.Name);
                FileInfo dataFile = new FileInfo(filePath.Substring(0, filePath.Length - 9) + ".csv");
                List<(int inDataIndex, string name)> annosList = new List<(int inDataIndex, string name)>();
                List<long> milliseconds = new List<long>();
                
                using (StreamReader sr = new StreamReader(dataFile.FullName, Encoding.UTF8))
                {
                    sr.ReadLine();

                    string line;
                    while ((line = sr.ReadLine()) != null)
                        milliseconds.Add(long.Parse(line.Split(',')[1]));
                }
                
                Console.WriteLine("Data file processed");
                using (StreamReader sr = new StreamReader(annoFile.FullName, Encoding.UTF8))
                {
                    sr.ReadLine();

                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] lineData = line.Split(',');
                        if (!allowAnnos.Contains(lineData[3]) && !emotions.Contains(lineData[3]))
                            continue;

                        DateTime dateTime = Convert.ToDateTime(lineData[1]);
                        long millisecondsToFind = dateTime.Millisecond + 
                                                  dateTime.Second * 1000L +
                                                  dateTime.Minute * 1000L * 60L + 
                                                  dateTime.Hour * 1000L * 60L * 60L;

                        int nearestIndex = BinaryNearestIndex(milliseconds, millisecondsToFind);
                        annosList.Add((nearestIndex, lineData[3]));
                    }
                }

                Console.WriteLine("Anno file processed and connected");
                using (StreamReader sr = new StreamReader(dataFile.FullName, Encoding.UTF8))
                {
                    string[] header = sr.ReadLine().Split(',');

                    bool findedEnter = false;
                    int enterInDataIndex = -1;
                    string emotionInSeries = null;
                    List<int> pressInDataIndexes = new List<int>();
                    int lastInFileIndex = 0;
                    float emotionValue = 0;
                    bool isFillEmotion = false;

                    for (int i = 0; i < annosList.Count; i++)
                    {
                        if (annosList[i].name.Equals("enter") && emotions.Contains(annosList[i + 1].name))
                        {
                            findedEnter = true;
                            enterInDataIndex = annosList[i].inDataIndex;
                            emotionInSeries = annosList[i + 1].name;
                            emotionValue = 0f;
                        }

                        if (findedEnter && (annosList[i].name.Equals("press") || annosList[i].name.Equals("press1")))
                            pressInDataIndexes.Add(annosList[i].inDataIndex);

                        if (findedEnter && annosList[i].name.Equals("exit"))
                        {
                            findedEnter = false;
                            Console.WriteLine("");
                            Console.WriteLine("Found serial with index " + outPutIndex);
                            
                            // read one serial data
                            sr.SkipNexLines(enterInDataIndex - lastInFileIndex);
                            List<string> seriesData = sr.ReadNextLines(annosList[i].inDataIndex - enterInDataIndex + 1);
                            lastInFileIndex = annosList[i].inDataIndex + 1;
                            Console.WriteLine("Serial readed");
                            
                            // form one serial and label file
                            List<string> finallySeriesData = new List<string>();
                            List<string> labels = new List<string>();
                            for (int j = 0; j < seriesData.Count; j++)
                            {
                                if (pressInDataIndexes.Contains(j + enterInDataIndex))
                                {
                                    emotionValue = 1f;
                                    isFillEmotion = true;
                                }

                                if (j % 25 != 0)
                                    continue;
                                
                                if (isFillEmotion)
                                    finallySeriesData.Add(GetDataLine(header, seriesData[j].Split(',')));

                                if (isFillEmotion)
                                    labels.Add(GetLabelLine(emotionInSeries, emotionValue));
                                
                                emotionValue *= EMOTION_SCALE_PER_TIME;

                                if (emotionValue < 0.1f && isFillEmotion)
                                {
                                    File.WriteAllLines(OUTPUT_FOLDER_PATH + "\\" + subOutPutIndex + "_input.csv", finallySeriesData, new UTF8Encoding(false));
                                    File.WriteAllLines(OUTPUT_FOLDER_PATH + "\\" + subOutPutIndex + "_labels.csv", labels, new UTF8Encoding(false));
                                    subOutPutIndex++;
                                    isFillEmotion = false;
                                }
                            }
                            
                            outPutIndex++;
                            pressInDataIndexes.Clear();
                        }
                    }
                }
            }

            Console.WriteLine("Start rename");
            Random rnd = new Random();
            for (int i = 0; i < 10000; i++)
            {
                int i1 = rnd.Next(subOutPutIndex - 1);
                int i2 = rnd.Next(subOutPutIndex - 1);
                
                if (i1 == i2)
                    continue;
                
                File.Move(OUTPUT_FOLDER_PATH + "\\" + i1 + "_input.csv", OUTPUT_FOLDER_PATH + "\\" + i1 + "[_input.csv");
                File.Move(OUTPUT_FOLDER_PATH + "\\" + i1 + "_labels.csv", OUTPUT_FOLDER_PATH + "\\" + i1 + "[_labels.csv");
                File.Move(OUTPUT_FOLDER_PATH + "\\" + i2 + "_input.csv", OUTPUT_FOLDER_PATH + "\\" + i1 + "_input.csv");
                File.Move(OUTPUT_FOLDER_PATH + "\\" + i2 + "_labels.csv", OUTPUT_FOLDER_PATH + "\\" + i1 + "_labels.csv");
                File.Move(OUTPUT_FOLDER_PATH + "\\" + i1 + "[_input.csv", OUTPUT_FOLDER_PATH + "\\" + i2 + "_input.csv");
                File.Move(OUTPUT_FOLDER_PATH + "\\" + i1 + "[_labels.csv", OUTPUT_FOLDER_PATH + "\\" + i2 + "_labels.csv");
            }
            Console.WriteLine("Rename complete");
        }
        
        private static string GetDataLine(string[] header, string[] rawData)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < channelsList.Count; i++)
            {
                int channelIndex = Array.IndexOf(header, channelsList[i]);
                builder.Append(channelIndex == -1 ? "0" : rawData[channelIndex]);
                
                if (i < channelsList.Count - 1)
                    builder.Append(",");
            }

            return builder.ToString();
        }
        
        private static string GetLabelLine(string emotion, float value)
        {
            return value > 0.1f ? emotions.IndexOf(emotion) + 1 + "" : "0";
        }
        
        private static int BinaryNearestIndex(List<long> list, long value)
        {
            int leftBound = 0;
            int rightBound = list.Count - 1;

            while (true)
            {
                int newIndex = (leftBound + rightBound) / 2;
                long newValue = list[newIndex];

                if (newValue > value)
                {
                    if (newIndex == 0)
                        return 0;

                    if (list[newIndex - 1] < value)
                        return Math.Abs(list[newIndex - 1] - value) > Math.Abs(list[newIndex] - value) ? newIndex : newIndex - 1;

                    rightBound = newIndex;
                }
                else if (newValue < value)
                {
                    if (newIndex == list.Count - 1)
                        return list.Count - 1;
                
                    if (list[newIndex + 1] > value)
                        return Math.Abs(list[newIndex + 1] - value) > Math.Abs(list[newIndex] - value) ? newIndex : newIndex + 1;

                    leftBound = newIndex;
                }
                else
                    return newIndex;
            }
        }
    }

    static class Extensions
    {
        public static List<string> ReadNextLines(this StreamReader streamReader, int count)
        {
            List<string> lines = new List<string>();
            for (int i = 0; i < count; i++)
                lines.Add(streamReader.ReadLine());

            return lines;
        }

        public static void SkipNexLines(this StreamReader streamReader, int count)
        {
            for (int i = 0; i < count; i++)
                streamReader.ReadLine();
        }
    }
}