using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.DetectorAPI
{
    /// <summary>
    /// ControlValidationExceptionReason
    /// </summary>
    enum ControlValidationExceptionReason
    {
        /// <summary>
        /// InvalidValue
        /// </summary>
        InvalidValue,
        /// <summary>
        /// InvalidFormat
        /// </summary>
        InvalidFormat,
        /// <summary>
        /// InvalidRange
        /// </summary>
        InvalidRange,
        /// <summary>
        /// NoData
        /// </summary>
        NoData
    }

    /// <summary>
    /// ControlValidationException
    /// </summary>
    class ControlValidationException : Exception
    {        
        private string validatedItemName;   // Validated item name
        private ControlValidationExceptionReason reason;    // ControlValidationExceptionReason

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="validatedControl">Validated control</param>
        /// <param name="validatedItemName">Validated item name</param>
        public ControlValidationException(string validatedItemName)
            : this(validatedItemName, ControlValidationExceptionReason.InvalidValue)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="validatedControl">Validated control</param>
        /// <param name="validatedItemName">Validated item name</param>
        /// <param name="reason">Validated exception reason</param>
        public ControlValidationException(string validatedItemName, ControlValidationExceptionReason reason)
            : this(validatedItemName, reason, ReasonToString(reason))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="validatedControl">Validated control</param>
        /// <param name="validatedItemName">Validated item name</param>
        /// <param name="reason">Validated exception reason</param>
        /// <param name="message">Message</param>
        public ControlValidationException(string validatedItemName, ControlValidationExceptionReason reason, string message)
            : base(message)
        {
            //this.validatedControl = validatedControl;
            this.validatedItemName = validatedItemName;
            this.reason = reason;
        }

        /// <summary>
        /// Convert ValidationExceptionReason to string
        /// </summary>
        /// <param name="reason">ValidationExceptionReason</param>
        /// <returns>Converted value</returns>
        public static string ReasonToString(ControlValidationExceptionReason reason)
        {
            string ret;

            switch (reason)
            {
                case ControlValidationExceptionReason.InvalidFormat:
                    ret = "Invalid Format";
                    break;

                case ControlValidationExceptionReason.InvalidRange:
                    ret = "Invalid Range";
                    break;

                case ControlValidationExceptionReason.NoData:
                    ret = "No Data";
                    break;

                default:
                    ret = "Invalid Value";
                    break;
            }

            return ret;
        }

        /// <summary>
        /// Get ValudatedControl
        /// </summary>
        /*public Control ValidatedControl
        {
            get
            {
                return validatedControl;
            }
        }*/

        /// <summary>
        /// Get ValidatedItemName
        /// </summary>
        public string ValidatedItemName
        {
            get
            {
                return validatedItemName;
            }
        }

        /// <summary>
        /// Get ControlValidationExceptionReason
        /// </summary>
        public ControlValidationExceptionReason Reason
        {
            get
            {
                return reason;
            }
        }
    }
}
