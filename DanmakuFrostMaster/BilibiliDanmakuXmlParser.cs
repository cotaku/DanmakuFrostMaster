﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Windows.UI;

namespace Atelier39
{
    public static class BilibiliDanmakuXmlParser
    {
        public static List<DanmakuItem> GetDanmakuList(string xmlStr, IList<string> regexFilterList, bool mergeDuplicate, out uint totalCount, out uint filteredCount, out uint mergedCount)
        {
            totalCount = 0;
            filteredCount = 0;
            mergedCount = 0;
            List<DanmakuItem> list = new List<DanmakuItem>();
            Dictionary<string, List<DuplicatedDanmakuItem>> duplicatedDanmakuDict = new Dictionary<string, List<DuplicatedDanmakuItem>>();

            if (string.IsNullOrWhiteSpace(xmlStr))
            {
                return list;
            }

            bool isNewFormat = xmlStr.Contains("<oid>");

            // Old format: <d p="23.826000213623,1,25,16777215,1422201084,0,057075e9,757076900">我从未见过如此厚颜无耻之猴</d>
            // New format: <d p="30845767597424709,0,59065,1,25,16777215,1585975807,0,8317333c">啊啊啊啊啊啊啊啊</d>
            // Mode 7 format: <d p="3782563342,0,274462,7,20,10027263,1504375326,0,a77032eb">[0,0.5,"1-1",3,"为了那个傻傻的放电妹",0,0,0.99,0.5,3000,0,true,"幼圆",1]</d>
            MatchCollection matchCollection = Regex.Matches(xmlStr, @"<d p=""(?<tag>.+?)"">(?<content>[\s\S]*?)</d>");
            foreach (Match match in matchCollection)
            {
                totalCount++;

                string tagStr = match.Groups["tag"].Value;
                string contentStr = match.Groups["content"].Value;
                DanmakuMode danmakuMode = DanmakuMode.Unknown;

                if (string.IsNullOrWhiteSpace(tagStr) || string.IsNullOrWhiteSpace(contentStr))
                {
                    filteredCount++;
                    continue;
                }
                contentStr = WebUtility.HtmlDecode(contentStr).Replace("/n", "\n").Replace("\\n", "\n").Trim();

                if (mergeDuplicate)
                {
                    string[] pArray = tagStr.Split(',');
                    if (pArray.Length >= 4)
                    {
                        if (int.TryParse(pArray[isNewFormat ? 3 : 1], out int mode) && double.TryParse(pArray[isNewFormat ? 2 : 0], out double time))
                        {
                            danmakuMode = (DanmakuMode)mode;
                            if ((danmakuMode == DanmakuMode.Rolling || danmakuMode == DanmakuMode.Top || danmakuMode == DanmakuMode.Bottom) && time >= 0)
                            {
                                uint startMs = (uint)(isNewFormat ? time : time * 1000);

                                if (!duplicatedDanmakuDict.ContainsKey(contentStr))
                                {
                                    duplicatedDanmakuDict.Add(contentStr, new List<DuplicatedDanmakuItem>() { new DuplicatedDanmakuItem() { StartMs = startMs, Count = 1 } });
                                }
                                else
                                {
                                    bool merged = false;
                                    List<DuplicatedDanmakuItem> duplicatedDanmakuList = duplicatedDanmakuDict[contentStr];
                                    foreach (DuplicatedDanmakuItem duplicatedDanmaku in duplicatedDanmakuList)
                                    {
                                        if (Math.Abs((int)(startMs - duplicatedDanmaku.StartMs)) <= 20000) // Merge duplicate danmaku in timeframe of 20s
                                        {
                                            merged = true;
                                            duplicatedDanmaku.Count++;
                                            break;
                                        }
                                    }
                                    if (merged)
                                    {
                                        //Debug.WriteLine($"Warning: merged danmaku: {contentStr} at {startMs}");
                                        mergedCount++;
                                        continue;
                                    }
                                    else
                                    {
                                        duplicatedDanmakuList.Add(new DuplicatedDanmakuItem() { StartMs = startMs, Count = 1 });
                                    }
                                }
                            }
                        }
                    }
                }

                if (danmakuMode != DanmakuMode.Advanced && danmakuMode != DanmakuMode.Subtitle && regexFilterList != null && regexFilterList.Count > 0)
                {
                    bool filtered = false;
                    foreach (string regexFilter in regexFilterList)
                    {
                        if (Regex.IsMatch(contentStr, regexFilter))
                        {
                            Debug.WriteLine($"Warning: filtered danmaku: {contentStr}");
                            filtered = true;
                            filteredCount++;
                            break;
                        }
                    }
                    if (filtered)
                    {
                        continue;
                    }
                }

                DanmakuItem item = ParseDanmakuItem(tagStr, contentStr, isNewFormat);
                if (item != null)
                {
                    list.Add(item);
                }
                else
                {
                    Debug.WriteLine($"Failed to create danmaku: {contentStr}");
                }
            }

            if (duplicatedDanmakuDict.Count > 0)
            {
                foreach (DanmakuItem item in list)
                {
                    if (item.Mode == DanmakuMode.Rolling || item.Mode == DanmakuMode.Top || item.Mode == DanmakuMode.Bottom)
                    {
                        if (duplicatedDanmakuDict.ContainsKey(item.Text))
                        {
                            List<DuplicatedDanmakuItem> duplicatedDanmakuList = duplicatedDanmakuDict[item.Text];
                            foreach (DuplicatedDanmakuItem duplicatedDanmaku in duplicatedDanmakuList)
                            {
                                if (duplicatedDanmaku.Count > 1 && item.StartMs == duplicatedDanmaku.StartMs)
                                {
                                    item.Text = $"{item.Text}\u00D7{duplicatedDanmaku.Count}";
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            BilibiliDanmakuSorter.Sort(list);

            return list;
        }

        public static List<DanmakuItem> GetSubtitleList(string jsonArrayStr)
        {
            List<DanmakuItem> list = new List<DanmakuItem>();
            if (!string.IsNullOrWhiteSpace(jsonArrayStr))
            {
                JObject jObject = JObject.Parse(jsonArrayStr);
                JToken bodyArray = jObject["body"];
                if (bodyArray != null)
                {
                    foreach (JToken jToken in bodyArray)
                    {
                        try
                        {
                            double fromMs = jToken["from"].ToObject<double>() * 1000;
                            double toMs = jToken["to"].ToObject<double>() * 1000;
                            if (toMs > fromMs)
                            {
                                string content = jToken["content"].ToString();
                                if (!string.IsNullOrWhiteSpace(content))
                                {
                                    DanmakuItem item = new DanmakuItem
                                    {
                                        Mode = DanmakuMode.Subtitle,
                                        StartMs = (uint)fromMs,
                                        DurationMs = (uint)(toMs - fromMs),
                                        Text = content,
                                        TextColor = Colors.White,
                                        BaseFontSize = DanmakuItem.DefaultBaseFontSize,
                                        HasOutline = false,
                                        AllowDensityControl = false
                                    };
                                    string[] sentenceArray = item.Text.Split('\n');
                                    for (int i = 0; i < sentenceArray.Length; i++)
                                    {
                                        sentenceArray[i] = sentenceArray[i].Trim();
                                    }
                                    item.Text = string.Join("\n", sentenceArray);
                                    list.Add(item);
                                }
                            }
                        }
                        catch
                        {
                            Debug.WriteLine($"Failed to parse subtitle entry: {jToken}");
                        }
                    }
                }
            }

            BilibiliDanmakuSorter.Sort(list);
            return list;
        }

        /// <summary>
        /// Return null if danmaku can't be parsed
        /// </summary>
        private static DanmakuItem ParseDanmakuItem(string tagStr, string content, bool isNewFormat)
        {
            string[] pArray = tagStr.Split(',');
            if (pArray.Length < 8)
            {
                return null;
            }

            try
            {
                DanmakuItem danmakuItem = new DanmakuItem
                {
                    Id = isNewFormat ? ulong.Parse(pArray[0]) : 0,
                    HasBorder = false,
                    Text = content,
                    TextColor = ParseColor(uint.Parse(pArray[isNewFormat ? 5 : 3]))
                };

                double startMs = isNewFormat ? double.Parse(pArray[2]) : double.Parse(pArray[0]) * 1000;
                if (startMs < 0)
                {
                    startMs = 0;
                }
                danmakuItem.StartMs = (uint)startMs;

                int mode = int.Parse(pArray[isNewFormat ? 3 : 1]);
                switch (mode)
                {
                    case (int)DanmakuMode.Rolling:
                        {
                            danmakuItem.Mode = DanmakuMode.Rolling;
                            break;
                        }
                    case (int)DanmakuMode.Bottom:
                        {
                            danmakuItem.Mode = DanmakuMode.Bottom;
                            break;
                        }
                    case (int)DanmakuMode.Top:
                        {
                            danmakuItem.Mode = DanmakuMode.Top;
                            break;
                        }
                    case (int)DanmakuMode.ReverseRolling:
                        {
                            danmakuItem.Mode = DanmakuMode.ReverseRolling;
                            break;
                        }
                    case (int)DanmakuMode.Advanced:
                        {
                            danmakuItem.Mode = DanmakuMode.Advanced;
                            break;
                        }
                    default:
                        {
                            Debug.WriteLine($"Skip unknown danmaku type: {mode}");
                            return null;
                        }
                }

                int fontSize = int.Parse(pArray[isNewFormat ? 4 : 2]);
                switch (danmakuItem.Mode)
                {
                    case DanmakuMode.Rolling:
                    case DanmakuMode.Bottom:
                    case DanmakuMode.Top:
                    case DanmakuMode.ReverseRolling:
                        {
                            fontSize -= fontSize % 2 == 1 ? 3 : 2;
                            break;
                        }
                    case DanmakuMode.Advanced:
                        {
                            fontSize += 4; // Experimental adjustment
                            break;
                        }
                }
                if (fontSize < 2)
                {
                    fontSize = 2;
                }
                danmakuItem.BaseFontSize = fontSize;

                if (danmakuItem.Mode == DanmakuMode.Advanced)
                {
                    if (!content.StartsWith("[") || !content.EndsWith("]"))
                    {
                        return null;
                    }

                    danmakuItem.AllowDensityControl = false;

                    string[] valueArray;
                    try
                    {
                        JArray jArray = JArray.Parse(content);
                        valueArray = new string[jArray.Count];
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            valueArray[i] = jArray[i].ToString();
                        }

                        if (valueArray.Length < 5)
                        {
                            return null;
                        }
                        danmakuItem.Text = WebUtility.HtmlDecode(valueArray[4]).Replace("/n", "\n").Replace("\\n", "\n");
                        if (string.IsNullOrWhiteSpace(danmakuItem.Text))
                        {
                            return null;
                        }

                        danmakuItem.StartX = string.IsNullOrWhiteSpace(valueArray[0]) ? 0f : float.Parse(valueArray[0]);
                        danmakuItem.StartY = string.IsNullOrWhiteSpace(valueArray[1]) ? 0f : float.Parse(valueArray[1]);
                        danmakuItem.EndX = danmakuItem.StartX;
                        danmakuItem.EndY = danmakuItem.StartY;

                        string[] opacitySplit = valueArray[2].Split('-');
                        danmakuItem.StartAlpha = (byte)(Math.Max(float.Parse(opacitySplit[0]), 0) * byte.MaxValue);
                        danmakuItem.EndAlpha = opacitySplit.Length > 1 ? (byte)(Math.Max(float.Parse(opacitySplit[1]), 0) * byte.MaxValue) : danmakuItem.StartAlpha;

                        danmakuItem.DurationMs = (ulong)(float.Parse(valueArray[3]) * 1000);
                        danmakuItem.TranslationDurationMs = danmakuItem.DurationMs;
                        danmakuItem.TranslationDelayMs = 0;
                        danmakuItem.AlphaDurationMs = danmakuItem.DurationMs;
                        danmakuItem.AlphaDelayMs = 0;

                        if (valueArray.Length >= 7)
                        {
                            danmakuItem.RotateZ = string.IsNullOrWhiteSpace(valueArray[5]) ? 0f : float.Parse(valueArray[5]);
                            danmakuItem.RotateY = string.IsNullOrWhiteSpace(valueArray[6]) ? 0f : float.Parse(valueArray[6]);
                        }
                        else
                        {
                            danmakuItem.RotateZ = 0f;
                            danmakuItem.RotateY = 0f;
                        }

                        if (valueArray.Length >= 11)
                        {
                            danmakuItem.EndX = string.IsNullOrWhiteSpace(valueArray[7]) ? 0f : float.Parse(valueArray[7]);
                            danmakuItem.EndY = string.IsNullOrWhiteSpace(valueArray[8]) ? 0f : float.Parse(valueArray[8]);
                            if (!string.IsNullOrWhiteSpace(valueArray[9]))
                            {
                                danmakuItem.TranslationDurationMs = (ulong)(float.Parse(valueArray[9]));
                            }
                            if (!string.IsNullOrWhiteSpace(valueArray[10]))
                            {
                                string translationDelayValue = valueArray[10];
                                if (translationDelayValue == "０") // To be compatible with legacy style
                                {
                                    danmakuItem.TranslationDelayMs = 0;
                                }
                                else
                                {
                                    danmakuItem.TranslationDelayMs = (ulong)(float.Parse(translationDelayValue));
                                }
                            }
                        }

                        //if (valueArray.Length >= 12 && (valueArray[11].Equals("true", StringComparison.OrdinalIgnoreCase) || valueArray[11] == "1"))
                        //{
                        //    danmakuItem.HasOutline = false;
                        //}
                        //else
                        //{
                        //    danmakuItem.OutlineColor = danmakuItem.TextColor.R + danmakuItem.TextColor.G + danmakuItem.TextColor.B > 32 ? Colors.Black : Colors.White;
                        //    danmakuItem.OutlineColor.A = danmakuItem.TextColor.A;
                        //}
                        danmakuItem.HasOutline = false;

                        //if (valueArray.Length >= 13)
                        //{
                        //    string fontFamilyName = valueArray[12];
                        //    if (!string.IsNullOrWhiteSpace(fontFamilyName))
                        //    {
                        //        danmakuItem.FontFamilyName = fontFamilyName.Replace("\"", string.Empty);
                        //    }
                        //}
                        danmakuItem.FontFamilyName = "Consolas"; // Default monospaced font

                        danmakuItem.KeepDefinedFontSize = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to parse advanced mode danmaku: {content} Exception: {ex.Message}");
                        return null;
                    }
                }

                return danmakuItem;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to parse danmaku tag: {tagStr} Exception: {ex.Message}");
                return null;
            }
        }

        private static Color ParseColor(uint colorValue)
        {
            colorValue = colorValue & 0xFFFFFF; // Ingore alpha
            uint b = 0xFF & colorValue;
            uint g = (0xFF00 & colorValue) >> 8;
            uint r = (0xFF0000 & colorValue) >> 16;
            return Color.FromArgb(byte.MaxValue, (byte)r, (byte)g, (byte)b);
        }

        private class DuplicatedDanmakuItem
        {
            public uint StartMs;
            public uint Count;
        }

        private static class BilibiliDanmakuSorter
        {
            public static void Sort(IList<DanmakuItem> list)
            {
                Merge(list, 0, list.Count - 1);
            }

            private static void Merge(IList<DanmakuItem> list, int p, int r)
            {
                if (p < r)
                {
                    int mid = (p + r) / 2;
                    Merge(list, p, mid);
                    Merge(list, mid + 1, r);
                    MergeArray(list, p, mid, r);
                }
            }

            private static void MergeArray(IList<DanmakuItem> list, int p, int mid, int r)
            {
                DanmakuItem[] tmp = new DanmakuItem[r - p + 1];
                int i = p, j = mid + 1;
                int m = mid, n = r;
                int k = 0;

                while (i <= m && j <= n)
                {
                    if (list[i].StartMs < list[j].StartMs)
                    {
                        tmp[k++] = list[i++];
                    }
                    else if (list[i].StartMs > list[j].StartMs)
                    {
                        tmp[k++] = list[j++];
                    }
                    else if (list[i].Mode == DanmakuMode.Advanced)
                    {
                        // Compare Id
                        if (list[i].Id <= list[j].Id)
                        {
                            tmp[k++] = list[i++];
                        }
                        else
                        {
                            tmp[k++] = list[j++];
                        }
                    }
                    else
                    {
                        tmp[k++] = list[i++];
                    }
                }

                while (i <= m)
                {
                    tmp[k++] = list[i++];
                }

                while (j <= n)
                {
                    tmp[k++] = list[j++];
                }

                for (i = 0; i < r - p + 1; i++)
                {
                    list[p + i] = tmp[i];
                }
            }
        }
    }
}
