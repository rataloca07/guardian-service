using Google.Cloud.Firestore;

namespace GuardianService.Models
{
    [FirestoreData]
    public class Guardian
    {
        [FirestoreProperty]
        public string Id { get; set; }
        [FirestoreProperty]
        public string Email { get; set; }
        [FirestoreProperty]
        public string Password { get; set; }
        [FirestoreProperty]
        public string Nombre { get; set; }
        [FirestoreProperty]
        public string TokenDispositivo { get; set; } // Token para FCM
    }
}
