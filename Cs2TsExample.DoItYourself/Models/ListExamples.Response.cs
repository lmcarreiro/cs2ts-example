using System;
using System.Collections.Generic;
using System.Text;

namespace Cs2TsExample.DoItYourself.Models
{
    public sealed class ListExamplesResponse : Response
    {
        public bool SomeBool { get; set; }
        public int? NullableInteger { get; set; }
        public NestedClass NestedContent { get; set; }
        public AnotherClass[] NestedArray { get; set; }

        public sealed class NestedClass
        {
            public int Id { get; set; }
            public IDictionary<string, int> ValuesByName { get; set; }
        }

        public sealed class AnotherClass
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public decimal Value { get; set; }
        }
    }
}
