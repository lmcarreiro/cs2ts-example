using System;
using System.Collections.Generic;
using System.Text;

namespace Cs2TsExample.DoItYourself.Models
{
    public sealed class ListExamplesRequest : Request
    {
        public string FilterByName { get; set; }
        public bool OnlyActive { get; set; }
    }
}
