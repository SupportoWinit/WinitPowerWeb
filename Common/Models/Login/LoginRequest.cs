using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models.Login
{
    public class LoginRequest
    {
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("password")]
        public string Password { get; set; }
        [JsonProperty("doubleCheck")]
        public bool DoubleCheck { get; set; }
        [JsonProperty("superUserName")]
        public string SuperUserName { get; set; }
        [JsonProperty("superUserPassword")]
        public string SuperUserPassword { get; set; }
    }
}
