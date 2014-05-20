namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Representation of a fraction
    /// </summary>
    public class Fraction
    {
        /// <summary>
        /// Constructs new fraction
        /// </summary>
        public Fraction()
        {
        }

        /// <summary>
        /// Constructs new fraction with specified numerator and denominator
        /// </summary>
        /// <param name="num">Numerator</param>
        /// <param name="den">Denominator</param>
        public Fraction(double num, double den)
        {
            this.Num = num;
            this.Den = den;
        }

        /// <summary>
        /// Fraction's numerator
        /// </summary>
        public double Num { get; set; }

        /// <summary>
        /// Fraction's denominator
        /// </summary>
        public double Den { get; set; }

        // conversion from Fraction to double
        /// <summary>
        /// Converts given Fraction object to double
        /// </summary>
        /// <param name="f">Fraction object</param>
        /// <returns>Fraction converted to double</returns>
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
