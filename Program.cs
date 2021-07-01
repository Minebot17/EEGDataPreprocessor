﻿using System;
using System.Collections.Generic;
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
        
        public static void Main(string[] args)
        {
            int outPutIndex = START_OUTPUT_INDEX;
            string[] files = Directory.GetFiles(INPUT_FOLDER_PATH, "*.csv");

            foreach (string filePath in files)
            {
                FileInfo annoFile = new FileInfo(filePath);
                if (!annoFile.Name.EndsWith("_anno.csv"))
                    continue;

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

                using (StreamReader sr = new StreamReader(dataFile.FullName, Encoding.UTF8))
                {
                    sr.ReadLine();

                    bool findedEnter = false;
                    int enterInDataIndex = -1;
                    string emotionInSeries;
                    List<int> pressInDataIndexes = new List<int>();
                    int lastInFileIndex = 0;

                    for (int i = 0; i < annosList.Count; i++)
                    {
                        if (annosList[i].name.Equals("enter") && emotions.Contains(annosList[i + 1].name))
                        {
                            findedEnter = true;
                            enterInDataIndex = annosList[i].inDataIndex;
                            emotionInSeries = annosList[i + 1].name;
                        }

                        if (findedEnter && (annosList[i].name.Equals("press") || annosList[i].name.Equals("press1")))
                            pressInDataIndexes.Add(annosList[i].inDataIndex);

                        if (findedEnter && annosList[i].name.Equals("exit"))
                        {
                            findedEnter = false;
                            pressInDataIndexes.Clear();
                            
                            // read one serial data
                            sr.SkipNexLines(enterInDataIndex - lastInFileIndex);
                            List<string> seriesData = sr.ReadNextLines(annosList[i].inDataIndex - enterInDataIndex + 1);
                            lastInFileIndex = annosList[i].inDataIndex + 1;
                            
                            // form one serial file
                            List<string> finallySeriesData = new List<string>();
                            for (int j = 0; j < seriesData.Count; j++)
                            {
                                string cutted = seriesData[j].Substring(seriesData[j].IndexOf(',') + 1);
                                finallySeriesData.Add(cutted.Substring(cutted.IndexOf(',') + 1));
                            }

                            File.WriteAllLines(OUTPUT_FOLDER_PATH + "\\" + outPutIndex + "_input.csv", finallySeriesData, Encoding.UTF8);
                            
                            // form labels file
                            // TODO

                            outPutIndex++;
                        }
                    }
                }
            }
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