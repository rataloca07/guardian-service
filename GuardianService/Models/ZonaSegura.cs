using Google.Cloud.Firestore;

namespace GuardianService.Models
{

    [FirestoreData]
    public class ZonaSegura
    {
        [FirestoreProperty]
        public string Id { get; set; }
        [FirestoreProperty]
        public double LatitudCentro { get; set; }
        [FirestoreProperty]
        public double LongitudCentro { get; set; }
        [FirestoreProperty]
        public double Radio { get; set; }
        [FirestoreProperty]
        public string Descripcion { get; set; }
        [FirestoreProperty]
        public string PacienteId { get; set; }
    }
}
