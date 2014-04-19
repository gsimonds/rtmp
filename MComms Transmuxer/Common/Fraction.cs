namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Fraction
    {
        public Fraction()
        {
        }

        public Fraction(double num, double den)
        {
            this.Num = num;
            this.Den = den;
        }

        public double Num { get; set; }

        public double Den { get; set; }

        // conversion from Fraction to double
        public static implicit operator double(Fraction f)
        {
            if (f.Den == 0)
            {
                return 0;
            }
            else
            {
                return (double)f.Num / f.Den;
            }
        }
    }
}
