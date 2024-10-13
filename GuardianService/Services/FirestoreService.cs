using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GuardianService.Models;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.Text;
using Google.Cloud.Firestore.V1;
using Grpc.Auth;

namespace GuardianService.Services
{
    public class FirestoreService
    {
        private FirestoreDb _firestoreDb;

        public FirestoreService(IConfiguration configuration)
        {
            // Obtener credenciales desde la variable de entorno o archivo local
            var credentialJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL");
            GoogleCredential credential;

            if (string.IsNullOrEmpty(credentialJson))
            {
                // Si no hay credenciales en el entorno, intenta cargar desde un archivo local (solo para desarrollo)
                try
                {
                    credential = GoogleCredential.FromFile("Firebase/serviceAccountKey.json");
                }
                catch (FileNotFoundException ex)
                {
                    throw new Exception("No se encontró el archivo de credenciales y no hay credenciales en las variables de entorno. Asegúrate de que las credenciales estén correctamente configuradas.", ex);
                }
            }
            else
            {
                // Utiliza las credenciales de la variable de entorno en producción
                credential = GoogleCredential.FromJson(credentialJson);
            }

            // Verificar que el ID del proyecto de Firebase esté presente
            var firebaseProjectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                throw new Exception("El ID del proyecto de Firebase no está configurado. Asegúrate de que la variable de entorno 'FIREBASE_PROJECT_ID' esté correctamente configurada.");
            }

            // Crear el FirestoreClient utilizando FirestoreClientBuilder con las credenciales
            var firestoreClient = new FirestoreClientBuilder
            {
                ChannelCredentials = credential.ToChannelCredentials()
            }.Build();

            // Crear la instancia de FirestoreDb con el ID del proyecto
            _firestoreDb = FirestoreDb.Create(firebaseProjectId, firestoreClient);
        }

        // Registrar un nuevo guardián
        public async Task RegistrarGuardian(Guardian guardian)
        {
            DocumentReference docRef = _firestoreDb.Collection("Guardianes").Document(guardian.Id);
            await docRef.SetAsync(guardian);
        }

        // Obtener un guardián por su correo electrónico
        public async Task<Guardian> ObtenerGuardianPorEmail(string email)
        {
            Query query = _firestoreDb.Collection("Guardianes").WhereEqualTo("Email", email);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            if (snapshot.Count == 0) return null;

            Guardian guardian = snapshot.Documents[0].ConvertTo<Guardian>();
            return guardian;
        }

        // Obtener un guardián por su ID
        public async Task<Guardian> ObtenerGuardianPorId(string guardianId)
        {
            DocumentReference docRef = _firestoreDb.Collection("Guardianes").Document(guardianId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Guardian guardian = snapshot.ConvertTo<Guardian>();
                return guardian;
            }

            return null; // Si no existe el guardián
        }


        // Registrar un paciente
        public async Task RegistrarPaciente(Paciente paciente)
        {
            DocumentReference docRef = _firestoreDb.Collection("Pacientes").Document(paciente.Id);
            await docRef.SetAsync(paciente);
        }

        // Actualizar la ubicación y el ritmo cardíaco del paciente desde el dispositivo IoT
        public async Task ActualizarEstadoPaciente(string sim, double latitud, double longitud, int ritmoCardiaco)
        {
            Query query = _firestoreDb.Collection("Pacientes").WhereEqualTo("SIM", sim);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0) throw new KeyNotFoundException("Paciente no encontrado");

            DocumentReference docRef = snapshot.Documents[0].Reference;
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "Latitud", latitud },
                { "Longitud", longitud },
                { "RitmoCardiaco", ritmoCardiaco }
            });
        }

        // Obtener un paciente por su número de SIM
        public async Task<Paciente> ObtenerPacientePorSIM(string sim)
        {
            Query query = _firestoreDb.Collection("Pacientes").WhereEqualTo("SIM", sim);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            if (snapshot.Count == 0) return null;

            Paciente paciente = snapshot.Documents[0].ConvertTo<Paciente>();
            return paciente;
        }

        // Obtener un paciente por su ID
        public async Task<Paciente> ObtenerPacientePorId(string pacienteId)
        {
            DocumentReference docRef = _firestoreDb.Collection("Pacientes").Document(pacienteId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Paciente paciente = snapshot.ConvertTo<Paciente>();
                return paciente;
            }

            return null; // Si no existe el paciente
        }

        public async Task<Paciente> ObtenerPacientePorGuardianId(string guardianId)
        {
            Query query = _firestoreDb.Collection("Pacientes").WhereEqualTo("GuardianId", guardianId);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            if (snapshot.Count == 0) return null;

            Paciente paciente = snapshot.Documents[0].ConvertTo<Paciente>();
            return paciente;
        }


        // Registrar una nueva zona segura
        /*public async Task RegistrarZonaSegura(ZonaSegura zonaSegura)
        {
            DocumentReference docRef = _firestoreDb.Collection("ZonasSeguras").Document(zonaSegura.Id);
            await docRef.SetAsync(zonaSegura);
        }*/


        public async Task RegistrarZonaSegura(ZonaSegura zonaSegura)
        {
            // Si no se ha proporcionado un Id, permite que Firestore genere uno automáticamente
            if (string.IsNullOrEmpty(zonaSegura.Id))
            {
                DocumentReference docRef = _firestoreDb.Collection("ZonasSeguras").Document();
                zonaSegura.Id = docRef.Id; // Asigna el Id generado a la zonaSegura
                await docRef.SetAsync(zonaSegura);
            }
            else
            {
                DocumentReference docRef = _firestoreDb.Collection("ZonasSeguras").Document(zonaSegura.Id);
                await docRef.SetAsync(zonaSegura);
            }
        }

        public async Task ModificarZonaSegura(ZonaSegura zonaSegura)
        {
            Query query = _firestoreDb.Collection("ZonasSeguras").WhereEqualTo("Id", zonaSegura.Id);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            if (snapshot.Count == 0) throw new KeyNotFoundException("Zona segura no encontrada");

            DocumentReference docRef = snapshot.Documents[0].Reference;
            Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "LatitudCentro", zonaSegura.LatitudCentro },
                    { "LongitudCentro", zonaSegura.LongitudCentro },
                    { "Radio", zonaSegura.Radio },
                    { "Descripcion", zonaSegura.Descripcion }
                };

            await docRef.UpdateAsync(updates);
        }


        public async Task EliminarZonaSegura(string zonaId)
        {
            Query query = _firestoreDb.Collection("ZonasSeguras").WhereEqualTo("Id", zonaId);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            if (snapshot.Count == 0) throw new KeyNotFoundException("Zona segura no encontrada");

            DocumentReference docRef = snapshot.Documents[0].Reference;
            await docRef.DeleteAsync();
        }

        // Verificar si un paciente está fuera de la zona segura
        public async Task<bool> PacienteFueraDeZonaSegura(string pacienteId, double latitudPaciente, double longitudPaciente)
        {
            var zonas = await ObtenerZonasSegurasPorPaciente(pacienteId);
            foreach (var zona in zonas)
            {
                double distancia = CalcularDistancia(latitudPaciente, longitudPaciente, zona.LatitudCentro, zona.LongitudCentro);
                if (distancia <= zona.Radio)
                {
                    return false; // Paciente está dentro de la zona segura
                }
            }
            return true; // Paciente está fuera de todas las zonas seguras
        }

        // Enviar notificación al guardián cuando el paciente esté fuera de la zona segura
        /*public async Task EnviarNotificacionAlerta(string tokenDispositivo, string titulo, string mensaje)
        {
            var message = new
            {
                to = tokenDispositivo,
                notification = new
                {
                    title = titulo,
                    body = mensaje
                },
                priority = "high"
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "key=RsJ-7IMrLZN7Sl-4tOBVouWAw04x_cVOiu6x5bhkVBE");

            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(message), System.Text.Encoding.UTF8, "application/json");
            await client.PostAsync("https://fcm.googleapis.com/fcm/send", content);
        }*/

        /*public async Task EnviarNotificacionAlerta(string tokenDispositivo, string titulo, string mensaje)
        {
            // Cargar credenciales desde el archivo de cuenta de servicio
            //var credential = await GoogleCredential
             //   .FromFile("Firebase/serviceAccountKey.json")
             //  .CreateScoped("https://www.googleapis.com/auth/firebase.messaging")
             //   .UnderlyingCredential
              //  .GetAccessTokenForRequestAsync();
            //var credentialJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL");
            GoogleCredential credential;

            if (string.IsNullOrEmpty(credentialJson))
            {
                credential = await GoogleCredential
                    .FromFile("Firebase/serviceAccountKey.json")
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging")
                    .UnderlyingCredential
                    .GetAccessTokenForRequestAsync();
            }
            else
            {
                credential = await GoogleCredential
                    .FromJson(credentialJson)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging")
                    .UnderlyingCredential
                    .GetAccessTokenForRequestAsync();
            }

            // Crear el mensaje de la notificación
            var message = new
            {
                message = new
                {
                    token = tokenDispositivo,
                    notification = new
                    {
                        title = titulo,
                        body = mensaje
                    }
                }
            };

            // Serializar el mensaje a JSON
            var jsonMessage = JsonConvert.SerializeObject(message);

            using var client = new HttpClient();
            // Añadir el token OAuth a la cabecera de autorización
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {credential}");

            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            // Enviar la solicitud a la API HTTP V1 de FCM
            var response = await client.PostAsync("https://fcm.googleapis.com/v1/projects/guardian-b6940/messages:send", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error enviando notificación: {error}");
            }
        }*/

        public async Task EnviarNotificacionAlerta(string tokenDispositivo, string titulo, string mensaje)
        {
            // Cargar credenciales desde el archivo de cuenta de servicio o variable de entorno
            GoogleCredential credential;
            var credentialJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL");

            if (!string.IsNullOrEmpty(credentialJson))
            {
                credential = GoogleCredential.FromJson(credentialJson)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            }
            else
            {
                // Maneja la excepción si no puedes obtener el JSON de las credenciales
                throw new Exception("No se encontraron credenciales válidas para Firebase.");
            }

            // Obtener el token OAuth para autenticación
            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

            // Crear el mensaje de la notificación
            var message = new
            {
                message = new
                {
                    token = tokenDispositivo,
                    notification = new
                    {
                        title = titulo,
                        body = mensaje
                    }
                }
            };

            // Serializar el mensaje a JSON
            var jsonMessage = JsonConvert.SerializeObject(message);

            using var client = new HttpClient();
            // Añadir el token OAuth a la cabecera de autorización
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            // Enviar la solicitud a la API HTTP V1 de FCM
            var response = await client.PostAsync("https://fcm.googleapis.com/v1/projects/guardian-b6940/messages:send", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error enviando notificación: {error}");
            }
        }

        // Obtener las zonas seguras de un paciente
        public async Task<List<ZonaSegura>> ObtenerZonasSegurasPorPaciente(string pacienteId)
        {
            Query query = _firestoreDb.Collection("ZonasSeguras").WhereEqualTo("PacienteId", pacienteId);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<ZonaSegura> zonas = snapshot.Documents.Select(doc => doc.ConvertTo<ZonaSegura>()).ToList();
            return zonas;
        }

        // Calcular la distancia entre dos puntos geográficos (Haversine)
        private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3; // Radio de la Tierra en metros
            var phi1 = lat1 * Math.PI / 180;
            var phi2 = lat2 * Math.PI / 180;
            var deltaPhi = (lat2 - lat1) * Math.PI / 180;
            var deltaLambda = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                    Math.Cos(phi1) * Math.Cos(phi2) *
                    Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        public async Task ActualizarTokenDispositivo(string guardianId, string token)
        {
            DocumentReference docRef = _firestoreDb.Collection("Guardianes").Document(guardianId);
            await docRef.UpdateAsync(new Dictionary<string, object>
    {
        { "TokenDispositivo", token }
    });
        }
    }
}
