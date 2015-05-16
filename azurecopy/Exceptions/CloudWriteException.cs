using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy.Exceptions
{
    public class CloudWriteException : Exception
    {
        public CloudWriteException()
        : base()
        {
        }

        public CloudWriteException(string message)
            : base(message)
        {
        }

        public CloudWriteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }
}
