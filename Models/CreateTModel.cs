using System.Collections.Generic;

namespace LockLock.Models
{
    public class CreateTModel
    {
        public string roomID { get; set; }

        public List<string> dates { get; set; }

        public List<string> color { get; set; }
    }
}