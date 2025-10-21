﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlareSync
{
    internal class DnsChange
    {
        public int Order { get; set; }
        public string Action { get; set; } = "";
        public string FQDN { get; set; } = "";
        public string Type { get; set; } = "";
        public string Value { get; set; } = "";
        public int TTL { get; set; }
    }
}
