using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWebAPIConsumer.Models
{
    public class Contact
    {
        
        
        public Guid ID { get; set; }


        
        public string FirstName { get; set; }

        
        public string LastName { get; set; }

        public string EmailAddress { get; set; }

        public string BusinessPhone { get; set; }
        
    }
}
