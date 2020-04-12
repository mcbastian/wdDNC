using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace wdDB.Model
{
    public class ApplicationUser: IdentityUser
    {
        public string ApiKey { get; set; }
        public virtual ICollection<Job> Jobs { get; set; }
    }
}
