using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocationSharingLibCS
{
    internal class GoogleCookie
    {
        // Models a cookie.

        internal string Domain { get; }
        internal bool Flag { get; }
        internal string Path { get; }
        internal bool Secure { get; }
        internal int Expiry { get; }
        internal string Name { get; }
        internal string Value { get; set; }

        internal GoogleCookie(string content)
        {
            string[] data = content.Split();
            Domain = data[0];
            if (data[1] == "TRUE") Flag = true;
            else Flag = false;
            Path = data[2];
            if (data[3] == "TRUE") Secure = true;
            else Secure = false;
            Expiry = int.Parse(data[4]);
            Name = data[5];
            Value = data[6];
        }
    }
}
