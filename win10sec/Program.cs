/**
 * Author: James Dickson 2016
 * 
 * Purpose: Compares a GPO Export file (gpreport.xml) from Microsoft Security Compliance Manager with a file exported with the command secedit
 * for example: secedit /export /cfg test1.inf.
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Microsoft.GroupPolicy;

namespace win10sec
{
    class Program
    {        
        static void Main(string[] args)
        {
            // Constants
            const string STR_KEYSTART = "<q1:KeyName>";
            const string STR_KEYEND = "</q1:KeyName>";
            const string STR_SETTINGSTART = "<q1:SettingString>";
            const string STR_NUMBERSTART = "<q1:SettingNumber>";
            const string STR_SETTINGEND = "</q1:SettingNumber>";
            const string STR_NUMBEREND = "</q1:SettingString>";

            if (args.Length < 2)
            {
                Console.WriteLine("[-] ERROR: requires two input files\nExample: win10sec.exe <gpo-backup-file> <secedit-export-file>");
                return;
            }

            if (!(File.Exists(args[0]) && File.Exists(args[1])))
            {
                Console.WriteLine("[-] ERROR: All input files does not exist!");
                return;
            }

            // Parse arguments
            string strFilename = args[0];
            string strTemplateFile = args[1];

            // Read files
            string [] strFile = File.ReadAllLines(strFilename);
            string[] strGPResultXMLFile = File.ReadAllLines(strTemplateFile);

            // Variables
            System.Collections.Hashtable htRegistryValues = new System.Collections.Hashtable();
            System.Collections.Hashtable htCurrentRegistrySettings = new System.Collections.Hashtable();

            // Read the XML-file (gpreport.xml) that has been exported by Microsoft Security Compliance Manager (Export --> GPO Backup Folder)
            for (int i = 0; i < strGPResultXMLFile.Length; i++)
            {
                int pos = strGPResultXMLFile[i].IndexOf(STR_KEYSTART);

                if (pos >= 0)
                {
                    int endPos = strGPResultXMLFile[i].IndexOf(STR_KEYEND);

                    string strAdd = strGPResultXMLFile[i].Substring(pos + STR_KEYSTART.Length, endPos - pos - STR_KEYSTART.Length);

                    int x = i;

                    for (x = i; x < strGPResultXMLFile.Length; x++)
                    {                        
                        int posString = strGPResultXMLFile[x].IndexOf(STR_SETTINGSTART);
                        int posNumber = strGPResultXMLFile[x].IndexOf(STR_NUMBERSTART);

                        int max = Math.Max(posNumber, posString);
                        string strComp = STR_NUMBERSTART;
                        string strComp2 = STR_SETTINGEND;

                        if (posNumber < posString)
                        {
                            strComp = STR_SETTINGSTART;
                            strComp2 = STR_NUMBEREND;
                        }


                        string strTemp = strGPResultXMLFile[x];
                        int endPosMax = strTemp.IndexOf(strComp2);

                        if (max >= 0)
                        {
                            string strValue = strTemp.Substring(max + strComp.Length, endPosMax - max - strComp.Length);

                            htRegistryValues.Add(strAdd, strValue);
                            break;
                        }

                        
                    }

                    i = x;
                }
            }
            
            // Read the .inf-file that has been exported from the template.
            bool bRegistryMode = false;

            for (int i = 0; i < strFile.Length; i++)
            {
                if (bRegistryMode)
                {
                    if (strFile[i].IndexOf("[") >= 0)
                    {
                        bRegistryMode = false;
                    }
                    else
                    {

                        int pos = 0;
                        int endPos = strFile[i].IndexOf("=");

                        if (endPos > 0)
                        {

                            string strAdd = strFile[i].Substring(pos, endPos - pos);
                            string strValue = strFile[i].Substring(endPos + 3);

                            htCurrentRegistrySettings.Add(strAdd, strValue);
                        }
                        else
                        {
                            throw new FormatException();
                        }
                    }
                }
                else
                {
                    if (strFile[i].IndexOf("[Registry Values]") >= 0)
                    {
                        bRegistryMode = true;
                    }
                }
            }


            // Print out the difference between settings
            System.Collections.IDictionaryEnumerator en = htRegistryValues.GetEnumerator();

            while(en.MoveNext())
            {
                string strKey = (string) en.Key;
                string strVal = (string) en.Value;

                Console.Write(en.Key + "|");

                if (htCurrentRegistrySettings.ContainsKey(strKey))
                {
                    string strCurrentVal = (string)htCurrentRegistrySettings[strKey];

                    bool bEqual = (strCurrentVal == (string)htRegistryValues[strKey]);

                    Console.WriteLine((string)htRegistryValues[strKey] + "|" + strCurrentVal + "|" + bEqual.ToString());
                }
                else
                {
                    Console.WriteLine("||Missing");
                }
            }

        }
    }
}
