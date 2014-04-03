namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class Fraction
    {
        public Fraction()
        {
        }

        public Fraction(int num, int den)
        {
            this.Num = num;
            this.Den = den;
        }

        public int Num { get; set; }

        public int Den { get; set; }

        // conversion from Fraction to double
        public static implicit operator double(Fraction f)
        {
            return (double)f.Num / f.Den;
        }
    }
}
