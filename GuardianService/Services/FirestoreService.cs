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
            var credentialJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL");
            GoogleCredential credential;

            if (string.IsNullOrEmpty(credentialJson))
            {
                // Fallback para desarrollo local
                credential = GoogleCredential.FromFile("Firebase/serviceAccountKey.json");
            }
            else
            {
                credential = GoogleCredential.FromJson(credentialJson);
            }

            // Crear el FirestoreClient utilizando FirestoreClientBuilder
            var firestoreClient = new FirestoreClientBuilder
            {
                ChannelCredentials = credential.ToChannelCredentials()
            }.Build();

            _firestoreDb = FirestoreDb.Create(Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID"), firestoreClient);
        }


        // Registrar un nuevo guardián
        public async Task RegistrarGuardian(Guardian guardian)
        {
            /*DocumentReference docRef = _firestoreDb.Collection("Guardianes").Document(guardian.Id);
            await docRef.SetAsync(guardian);*/
            if (string.IsNullOrEmpty(guardian.Id))
            {
                DocumentReference docRef = _firestoreDb.Collection("Guardianes").Document();
                guardian.Id = docRef.Id; // Asigna el Id generado a la zonaSegura
                await docRef.SetAsync(guardian);
            }
            else
            {
                DocumentReference docRef = _firestoreDb.Collection("Guardianes").Document(guardian.Id);
                await docRef.SetAsync(guardian);
            }
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
        /*public async Task RegistrarPaciente(Paciente paciente)
        {
            //DocumentReference docRef = _firestoreDb.Collection("Pacientes").Document(paciente.Id);
            //await docRef.SetAsync(paciente);
            if (string.IsNullOrEmpty(paciente.Id))
            {
                DocumentReference docRef = _firestoreDb.Collection("Pacientes").Document();
                paciente.Id = docRef.Id; 
                await docRef.SetAsync(paciente);
            }
            else
            {
                DocumentReference docRef = _firestoreDb.Collection("Pacientes").Document(paciente.Id);
                await docRef.SetAsync(paciente);
            }
        }*/


        public async Task<Paciente> RegistrarPaciente(Paciente paciente)
        {
            if (string.IsNullOrEmpty(paciente.Id))
            {
                // Generar un nuevo ID si no se proporciona uno
                DocumentReference docRef = _firestoreDb.Collection("Pacientes").Document();
                paciente.Id = docRef.Id;  // Asignar el ID generado al paciente
                await docRef.SetAsync(paciente);
            }
            else
            {
                // Si ya tiene un ID, usarlo directamente
                DocumentReference docRef = _firestoreDb.Collection("Pacientes").Document(paciente.Id);
                await docRef.SetAsync(paciente);
            }

            // Retornar el paciente con el ID asignado
            return paciente;
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

        public async Task<bool> ModificarPaciente(Paciente paciente)
        {
            try
            {
                DocumentReference pacienteDocRef = _firestoreDb.Collection("Pacientes").Document(paciente.Id);

                Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "Nombre", paciente.Nombre },
                { "SIM", paciente.SIM }
                // Añade aquí cualquier campo adicional que se pueda modificar
            };

                await pacienteDocRef.UpdateAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al modificar paciente: {ex.Message}");
                return false;
            }
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
        /*public async Task<bool> PacienteFueraDeZonaSegura(string pacienteId, double latitudPaciente, double longitudPaciente)
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
        }*/
        public async Task<bool> PacienteFueraDeZonaSegura(string pacienteId, double latitudPaciente, double longitudPaciente)
        {
            var zonas = await ObtenerZonasSegurasPorPaciente(pacienteId);

            // Verificar si no hay zonas registradas para este paciente
            if (zonas == null || zonas.Count == 0)
            {
                // Si no hay zonas seguras registradas, podemos decidir que el paciente está "fuera de zona segura"
                return true; // Paciente fuera de zona segura por no haber zonas definidas
            }

            // Si hay zonas seguras, calcular la distancia del paciente a cada zona
            foreach (var zona in zonas)
            {
                double distancia = CalcularDistancia(latitudPaciente, longitudPaciente, zona.LatitudCentro, zona.LongitudCentro);
                if (distancia <= zona.Radio)
                {
                    return false; // Paciente está dentro de la zona segura
                }
            }

            // Si no está dentro de ninguna zona segura
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

        /*public async Task EnviarNotificacionAlerta(string tokenDispositivo, string titulo, string mensaje)
        {
            // Cargar credenciales desde el archivo de cuenta de servicio o variable de entorno
            GoogleCredential credential;
            var credentialJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL");

            if (!string.IsNullOrEmpty(credentialJson))
            {
                Console.WriteLine("credentialJson: " + credentialJson);
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
        }*/
        public async Task EnviarNotificacionAlerta(string tokenDispositivo, string titulo, string mensaje)
        {
            Console.WriteLine("Entró a EnviarNotificacionAlerta");
            Console.WriteLine("tokenDispositivo: " + JsonConvert.SerializeObject(tokenDispositivo));
            Console.WriteLine("titulo: " + JsonConvert.SerializeObject(titulo));
            Console.WriteLine("mensaje: " + JsonConvert.SerializeObject(mensaje));
            GoogleCredential credential;
            var credentialJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL");

            if (!string.IsNullOrEmpty(credentialJson))
            {
                credential = GoogleCredential.FromJson(credentialJson)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            }
            else
            {
                throw new Exception("No se encontraron credenciales válidas para Firebase.");
            }

            // Codifica los títulos y mensajes para asegurarte de que están en el formato correcto
            titulo = System.Net.WebUtility.HtmlEncode(titulo);
            mensaje = System.Net.WebUtility.HtmlEncode(mensaje);


            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

            var message = new
            {
                message = new
                {
                    token = tokenDispositivo,
                    notification = new
                    {
                        title = titulo,
                        body = mensaje,
                        sound = "default"
                    }
                }
            };

            var jsonMessage = JsonConvert.SerializeObject(message);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            // Agrega un log para saber que se está intentando enviar la solicitud
            Console.WriteLine("Enviando notificación a FCM con el mensaje: " + jsonMessage);

            var response = await client.PostAsync("https://fcm.googleapis.com/v1/projects/guardian-b6940/messages:send", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error enviando notificación: {error}");
            }

            // Log para confirmar que la notificación fue enviada con éxito
            Console.WriteLine("Notificación enviada exitosamente.");
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

        /*public async Task ActualizarTokenDispositivo(string guardianId, string token)
        {
            DocumentReference docRef = _firestoreDb.Collection("Guardianes").Document(guardianId);
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "TokenDispositivo", token }
            });
        }*/

        /*public async Task ActualizarTokenDispositivo(string guardianId, string token)
        {
            try
            {
                // Busca el guardián en la base de datos
                var guardianDoc = await _firestoreDb.Collection("Guardianes").Document(guardianId).GetSnapshotAsync();

                if (guardianDoc.Exists)
                {
                    var tokenDispositivoActual = guardianDoc.GetValue<string>("TokenDispositivo");

                    // Actualizamos el token, incluso si ya existía uno
                    if (!string.IsNullOrEmpty(tokenDispositivoActual))
                    {
                        Console.WriteLine($"Token anterior: {tokenDispositivoActual}. Se actualizará al nuevo token: {token}");
                    }

                    // Actualizamos el token en Firebase
                    await guardianDoc.Reference.UpdateAsync("TokenDispositivo", token);
                    Console.WriteLine("Token de dispositivo actualizado correctamente en la base de datos");
                }
                else
                {
                    Console.WriteLine($"Guardían con ID {guardianId} no encontrado.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando token de dispositivo: {ex.Message}");
                throw;
            }
        }*/
        public async Task ActualizarTokenDispositivo(string guardianId, string token)
        {
            try
            {
                // Busca el guardián en la base de datos
                var guardianDoc = await _firestoreDb.Collection("Guardianes").Document(guardianId).GetSnapshotAsync();

                if (guardianDoc.Exists)
                {
                    // Verificamos si el campo 'TokenDispositivo' existe en el documento
                    if (guardianDoc.ContainsField("TokenDispositivo"))
                    {
                        var tokenDispositivoActual = guardianDoc.GetValue<string>("TokenDispositivo");

                        // Actualizamos el token, incluso si ya existía uno
                        if (!string.IsNullOrEmpty(tokenDispositivoActual))
                        {
                            Console.WriteLine($"Token anterior: {tokenDispositivoActual}. Se actualizará al nuevo token: {token}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("El campo 'TokenDispositivo' no existe. Se agregará por primera vez.");
                    }

                    // Actualizamos o agregamos el token en Firebase
                    await guardianDoc.Reference.UpdateAsync("TokenDispositivo", token);
                    Console.WriteLine("Token de dispositivo actualizado correctamente en la base de datos");
                }
                else
                {
                    Console.WriteLine($"Guardían con ID {guardianId} no encontrado.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando token de dispositivo: {ex.Message}");
                throw;
            }
        }

        /*public async Task EliminarTokenDispositivo(string guardianId)
        {
            // Lógica para eliminar el token de dispositivo en Firebase o tu base de datos
            var guardianDoc = _firestoreDb.Collection("Guardianes").Document(guardianId);
            var updates = new Dictionary<string, object>
                {
                    { "TokenDispositivo", FieldValue.Delete }
                };
            await guardianDoc.UpdateAsync(updates);
        }*/

        public async Task EliminarTokenDispositivo(string guardianId)
        {
            try
            {
                // Buscar el documento del guardián
                var guardianDoc = await _firestoreDb.Collection("Guardianes").Document(guardianId).GetSnapshotAsync();

                if (guardianDoc.Exists)
                {
                    // Verificar si el campo 'TokenDispositivo' existe antes de intentar eliminarlo
                    if (guardianDoc.ContainsField("TokenDispositivo"))
                    {
                        // Crear un diccionario de actualizaciones para eliminar el campo
                        var updates = new Dictionary<string, object>
                {
                    { "TokenDispositivo", FieldValue.Delete }
                };

                        // Ejecutar la actualización para eliminar el campo
                        await guardianDoc.Reference.UpdateAsync(updates);
                        Console.WriteLine("Token de dispositivo eliminado correctamente.");
                    }
                    else
                    {
                        Console.WriteLine("El campo 'TokenDispositivo' no existe en este documento, no es necesario eliminarlo.");
                    }
                }
                else
                {
                    Console.WriteLine($"Guardían con ID {guardianId} no encontrado.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar el token de dispositivo: {ex.Message}");
                throw;
            }
        }
    }
}
