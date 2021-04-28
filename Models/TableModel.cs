using System.Collections.Generic;
using System;

namespace LockLock.Models
{
    public class TableModel
    {
        public string objName { get; set; }

        public string timeLength { get; set; }

        public List<string> name { get; set; }

        public Tuple<string, uint>[,] table { get; set; }
    }
}