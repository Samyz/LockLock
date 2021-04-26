
using Google.Cloud.Firestore;

namespace LockLock.Models
{
    [FirestoreData]
    public class UserModel
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