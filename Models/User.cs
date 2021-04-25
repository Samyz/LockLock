using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace LockLock.Models
{
    [FirestoreData]
    public class User
    {
        public string UserId { get; set; }

        [FirestoreProperty]
        public string email { get; set; }

        [FirestoreProperty]
        public string firstname { get; set; }

        [FirestoreProperty]
        public string lastname { get; set; }

        [FirestoreProperty]
        public string tel { get; set; }
    }
}