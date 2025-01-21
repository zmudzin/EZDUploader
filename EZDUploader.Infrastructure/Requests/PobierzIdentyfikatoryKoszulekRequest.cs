using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Infrastructure.Requests
{

    public class PobierzIdentyfikatoryKoszulekRequest
    {
        public int IdPracownikaWlasciciela { get; set; }
    }

    public class PobierzIdentyfikatoryKoszulekResponse
    {
        public List<int> Koszulki { get; set; }
    }
}
