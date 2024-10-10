using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonCfgNet;

public class JsonCfgException(string message, Exception? inner = null) : Exception(message, inner)
{
}
