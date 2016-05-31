﻿using System;
using System.IO;
using System.Collections.Generic;

namespace  ContentTool.Builder
{
    [Serializable()]
    public class BuildInfo
    {
        public BuildInfo(string importPath,string input,string output=null)
            :this()
        {
            this.InputFile = input;
            this.OutputFile = output;
            string inputFile = Path.Combine(importPath,InputFile);
            InputTime=new FileInfo(inputFile).LastWriteTimeUtc;
        }
        public BuildInfo()
        {
            Dependencies = new List<string>();
        }
        public void Refresh(string importPath)
        {
            string inputFile = Path.Combine(importPath,InputFile);
            InputTime=new FileInfo(inputFile).LastWriteTimeUtc;
        }
        public void BuildDone(string outputPath)
        {
            string outputFile = Path.Combine(outputPath,OutputFile);
            if (File.Exists(outputFile))
                OutputTime=new FileInfo(outputFile).LastWriteTimeUtc;
        }
        
        public string InputFile{get;private set;}
        public string OutputFile{get;private set;}
        public DateTime InputTime{get;private set;}
        public DateTime OutputTime{get;private set;}

        public bool IsBuilt(string outputPath)
        {
            string outputFile = Path.Combine(outputPath,OutputFile);
            if (File.Exists(outputFile))
                return true;
            return false;
        }

        internal bool NeedsRebuild(string importPath,string outputPath)
        {
            string inputFile = Path.Combine(importPath,InputFile),outputFile=Path.Combine(outputPath,OutputFile);
            if (!File.Exists(inputFile)||InputTime != new FileInfo(inputFile).LastWriteTimeUtc || !File.Exists(outputFile) || OutputTime != new FileInfo(outputFile).LastWriteTimeUtc)
                return true;

            return false;
        }

        public List<string> Dependencies{get;private set;}
    }
}

