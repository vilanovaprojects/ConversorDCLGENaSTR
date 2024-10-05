


using System.Diagnostics;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    static void Main(string[] args)
    {

        // TÍTULO
        Console.WriteLine("Conversor DCLGEN to STR.  Created by Moisés Campaña");
        Console.WriteLine("OJO! No convertir la BDOIN1BEO");



        // 1. Solicitar la ruta de origen de los archivos DCLGEN
        Console.WriteLine("Por favor, ingrese la ruta de origen de los archivos DCLGEN:");
        string rutaOrigenDCLGEN = Console.ReadLine();

        // Verificar que la ruta de origen exista
        if (string.IsNullOrWhiteSpace(rutaOrigenDCLGEN) || !Directory.Exists(rutaOrigenDCLGEN))
        {
            Console.WriteLine("La ruta de origen no es válida o no existe.");
            return;
        }

        // 2. Crear la ruta de destino como subcarpeta "STR" en la carpeta de origen
        string rutaDestinoSTR = Path.Combine(rutaOrigenDCLGEN, "STR");

        // Crear la carpeta "STR" si no existe
        if (!Directory.Exists(rutaDestinoSTR))
        {
            Directory.CreateDirectory(rutaDestinoSTR);
            Console.WriteLine($"Carpeta de destino creada: {rutaDestinoSTR}");
        }

        // 3. Obtener todos los archivos DCLGEN en la carpeta de origen
        string[] archivosDCLGEN = Directory.GetFiles(rutaOrigenDCLGEN, "*");

        
        if (archivosDCLGEN.Length == 0)
        {
            Console.WriteLine("No se encontraron archivos DCLGEN en la ruta de origen.");
            return;
        }



        // 4. Procesar cada archivo DCLGEN
        foreach (var archivoDCLGEN in archivosDCLGEN)
        {
            // Obtener el nombre del archivo (sin la ruta completa)
            string nombreArchivo = Path.GetFileName(archivoDCLGEN);
            Console.WriteLine($"Procesando: {nombreArchivo}...");

            // Obtener el nombre del archivo .STR
            string archivoCBL = Path.Combine(rutaDestinoSTR, "B" + nombreArchivo + ".cbl");
            string archivoIDY = Path.Combine(rutaDestinoSTR, "B" + nombreArchivo + ".idy");
            string archivoSTR = Path.Combine(rutaDestinoSTR, "B" + nombreArchivo + ".str");


            // Diccionario para almacenar los campos y su propiedad de nulabilidad (NULL, NOTNULL, vacío)
            Dictionary<string, string> camposConNulabilidad = new Dictionary<string, string>();

            bool enSeccionSQL = false;
            bool enSeccionCobol = false;
            List<string> salidaCobol = new List<string>();
            salidaCobol.Add("       IDENTIFICATION DIVISION.");
            salidaCobol.Add($"       PROGRAM-ID. {nombreArchivo}.");
            salidaCobol.Add("       ENVIRONMENT DIVISION.");
            salidaCobol.Add("       DATA DIVISION.");
            salidaCobol.Add("       WORKING-STORAGE SECTION.");
            salidaCobol.Add("       01 TABLA.");


            // Leer el archivo línea por línea
            foreach (string linea in File.ReadLines(archivoDCLGEN))
            {
                int totalpic = 0;

                string lineaLimpia = linea.Trim();

                // Verificar si estamos en la sección SQL (buscando EXEC SQL DECLARE TABLE)
                if (lineaLimpia.Contains("EXEC SQL DECLARE"))
                {
                    enSeccionSQL = true;
                    continue;
                }

                // Si estamos en la sección SQL y encontramos END-EXEC, terminamos
                if (enSeccionSQL && lineaLimpia.Contains("END-EXEC"))
                {
                    enSeccionSQL = false;
                    continue;
                }

                // Extraer los campos SQL y su información de nulabilidad
                if (enSeccionSQL)
                {
                    // Dividir la línea por espacios para obtener las palabras
                    var partes = lineaLimpia.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    // Verificar si la primera palabra es "(" o ")"
                    string nombreCampo;
                    if (partes[0] == "(")
                    {
                        // Verificar si el texto contiene "_"
                        if (partes[1].Contains("_"))
                        {
                            // Reemplazar "_" por "-"
                            partes[1] = partes[1].Replace("_", "-");
                        }

                        nombreCampo = partes[1];
                    }
                    else
                    {
                        // Verificar si el texto contiene "_"
                        if (partes[0].Contains("_"))
                        {
                            // Reemplazar "_" por "-"
                            partes[0] = partes[0].Replace("_", "-");
                        }
                        nombreCampo = partes[0];
                    }

                    if (lineaLimpia.Contains("NOT NULL"))
                    {
                        camposConNulabilidad.Add(nombreCampo, "NOT NULL");
                    }
                    else
                    {
                        camposConNulabilidad.Add(nombreCampo, "NULL");

                    }

                }



                // Verificar si estamos en la sección COBOL (buscando COBOL DECLARATION)
                if (lineaLimpia.Contains("COBOL DECLARATION"))
                {
                    enSeccionCobol = true;
                    continue;
                }


                // Procesar la sección COBOL
                if (enSeccionCobol && Char.IsDigit(lineaLimpia[0]) && !lineaLimpia.StartsWith("01"))
                {

                    var partes = lineaLimpia.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    //Cambiar COMP-5 por comp-X
                    if (lineaLimpia.Contains("COMP-5") && partes.Length  > 2)
                    {
                        if (partes[3].StartsWith("S"))
                        {
                            // Si empieza por "S", quitamos la primera letra usando Substring
                            partes[3] = partes[3].Substring(1);
                        }

                        if (lineaLimpia.Contains("-LEN"))
                        {
                            salidaCobol.Add("              " + partes[0] + " " + partes[1] + "       PIC "
                            + partes[3] + " USAGE COMP-X.");
                        }
                        else if (lineaLimpia.Contains("-LEN"))
                        {
                            salidaCobol.Add("              " + partes[0] + " " + partes[1] + "      PIC "
                            + partes[3] + " USAGE COMP-X.");
                        }
                        else
                        {
                            salidaCobol.Add("           " + partes[0] + " " + partes[1] + "              PIC "
                            + partes[3] + " USAGE COMP-X.");
                        }
                        
                    }
                    else
                    {
                        salidaCobol.Add(linea);
                    }

                    

                    if (partes[1].Contains(".") || partes[1].Contains("-LEN"))
                    {
                        continue;
                    }
                    else if (partes[1].Contains("-TEXT"))
                    {
                        string nombreCampoSinText = partes[1].Replace("-TEXT", "");
                        if (camposConNulabilidad[nombreCampoSinText] == "NULL")
                        {
                            salidaCobol.Add("              " + partes[0] + " " + nombreCampoSinText + "-NULL      PIC X.");
                            
                        }

                    }
                    else if (camposConNulabilidad[partes[1]] == "NULL")
                    {
                        salidaCobol.Add("           " + partes[0] + " " + partes[1] + "-NULL         PIC X.");
                    }




                }

            }


            // Guarda la lista en el archivo
            using (StreamWriter writer = new StreamWriter(archivoCBL))
            {
                foreach (string linea in salidaCobol)
                {
                    writer.WriteLine(linea);
                }
            }



            Console.WriteLine("**************************   CBL  **************************");
            Console.WriteLine($"Ruta archivo: {archivoCBL}");


            TransformCBLaIDY(archivoCBL);
            Console.WriteLine("**************************   IDY  **************************");
            Console.WriteLine($"Ruta archivo: {archivoIDY}");


            TransformIDYaSTR(archivoIDY, archivoSTR);
            Console.WriteLine("**************************   STR  **************************");
            Console.WriteLine($"Ruta archivo: {archivoSTR}");


            //Borrado CBL IDY
            //Borrado(archivoCBL);
            Borrado(archivoIDY);
            Console.WriteLine("**************************   BORRADO CBL IDY  **************************");



        }
    }

    private static void Borrado(string archivo)
    {
        try
        {
            // Comprobar si el archivo existe antes de intentar borrarlo
            if (File.Exists(archivo))
            {
                // Borrar el archivo
                File.Delete(archivo);
                Console.WriteLine($"El archivo {archivo} ha sido eliminado.");
            }
            else
            {
                Console.WriteLine($"El archivo {archivo} no existe.");
            }
        }
        catch (Exception ex)
        {
            // Capturar cualquier error que ocurra al intentar borrar el archivo
            Console.WriteLine($"Error al eliminar el archivo: {ex.Message}");
        }
    }
    private static void TransformIDYaSTR(string archivoIDY, string archivoSTR)
    {
        // Retardo
        //Thread.Sleep(300);
        // Crear un nuevo proceso para ejecutar el comando COBOL
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c set PATH=%PATH%;E:\\Program Files (x86)\\Micro Focus\\Enterprise Developer\\bin " +
                        $"&& dfstrcl \"{archivoIDY}\" /d TABLA /o \"{archivoSTR}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Inicia el proceso
        Process process = new Process { StartInfo = startInfo };
        process.Start();

        process.WaitForExit();

        Console.WriteLine($"Convertido: {archivoIDY}");

    }

    private static void TransformCBLaIDY(string archivoCBL)
    {
        // Crear un nuevo proceso para ejecutar el comando COBOL
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c set PATH=%PATH%;E:\\Program Files (x86)\\Micro Focus\\Enterprise Developer\\bin && cobol \"{archivoCBL}\" anim;",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Inicia el proceso
        Process process = new Process { StartInfo = startInfo };
        process.Start();

        process.WaitForExit();

        Console.WriteLine($"Convertido: {archivoCBL}");
    }
}




