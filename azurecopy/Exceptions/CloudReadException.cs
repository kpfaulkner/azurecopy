using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy.Exceptions
{
    public class CloudReadException : Exception
    {
        public CloudReadException()
        : base()
        {
        }

        public CloudReadException(string message)
            : base(message)
        {
        }

        public CloudReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }
}
