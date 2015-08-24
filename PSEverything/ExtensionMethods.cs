using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PSEverything
{
    internal static class ExtensionMethods
    {
        public static T GetLParam<T>(this Message message)
        {
            return (T) message.GetLParam(typeof (T));
        }
    }
}
