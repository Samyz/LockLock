using Google.Cloud.Firestore;

namespace LockLock.Models
{
    [FirestoreData]
    public class SignUpModel
    {
        [FirestoreProperty]
        public string Email { get; set; }
        [FirestoreProperty]
        public string Firstname { get; set; }
        [FirestoreProperty]
        public string Lastname { get; set; }
        [FirestoreProperty]
        public string Tel { get; set; }
        public string Password { get; set; }
    }
}