﻿using System;
using DotNet.Cli.Flubu.Scripting.Analysis;
using DotNet.Cli.Flubu.Scripting.Processor;
using System.Reflection;

namespace DotNet.Cli.Flubu.Scripting.Processors
{
    public class ReferenceDirectiveProcessor : IDirectiveProcessor
    {
        public bool Process(AnalyserResult analyserResult, string line)
        {
            if (!line.StartsWith("//#ref"))
                return false;

            int dllIndex = line.IndexOf(" ", StringComparison.Ordinal);

            if (dllIndex < 0)
                return true;

            string dll = line.Substring(dllIndex);
            var type = Type.GetType(dll, true);
            
            analyserResult.References.Add(type.GetTypeInfo().Assembly.Location);
            return true;
        }
    }
}
