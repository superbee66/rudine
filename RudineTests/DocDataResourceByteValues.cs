﻿using System;
using Rudine.Tests.Properties;

namespace Rudine.Tests
{
    class DocDataSampleValues : NUnit.Framework.ValuesAttribute
    {
        public DocDataSampleValues() : base(nameof(Resources.BaseLineInfoPath2013), nameof(Resources.BaseLineOpenOffice4)) { }
    }

    class FileExtensionValues : NUnit.Framework.ValuesAttribute
    {
        public FileExtensionValues() : base("xsn", "pdf") { }
    }
    
    class DocFieldDataTypes: NUnit.Framework.ValueSourceAttribute {
        public DocFieldDataTypes(string sourceName) : base(sourceName) { }
        public DocFieldDataTypes(Type sourceType, string sourceName) : base(sourceType, sourceName) { }
    }

}