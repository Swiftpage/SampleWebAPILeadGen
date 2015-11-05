using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWebAPIConsumer.Models
{
    public class SignupModel
    {
        public string Name { get; set; }
        [Display(Name="Email Address")]
        public string EmailAddress { get; set; }
        [Display(Name="Phone Number")]
        public string PhoneNumber { get; set; }
    }
}
