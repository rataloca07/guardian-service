using Google.Cloud.Firestore;

namespace GuardianService.Models
{
    [FirestoreData]
    public class Paciente
    {
        [FirestoreProperty]
        public string Id { get; set; }
        [FirestoreProperty]
        public string SIM { get; set; }

        [FirestoreProperty]
        public string Nombre { get; set; }

        [FirestoreProperty]
        public double Latitud { get; set; }
        [FirestoreProperty]
        public double Longitud { get; set; }
        [FirestoreProperty]
        public int RitmoCardiaco { get; set; }
        [FirestoreProperty]
        public string GuardianId { get; set; }
    }
}
