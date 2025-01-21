using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/")]
    public class LoginRequest : IReturn<LoginResponse>
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string RoleOrganizacyjne { get; set; }
        public int IdJednostki { get; set; }
    }

    public class LoginResponse
    {
        public int IdPracownika { get; set; }
        public int IdStanowiska { get; set; }
    }
}
