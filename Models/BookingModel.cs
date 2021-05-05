using System;
using System.Collections.Generic;

namespace LockLock.Models
{

    public class BookingModel
    {
        public string BookingID { get; set; }
        public string Name { get; set; }
        public int Num { get; set; }
        public string RoomName { get; set; }
        public List<DateTime> timeList { get; set; }
    }

}