using DocumentalManager.Models;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace DocumentalManager.Services
{
    public class ExcelService
    {
        public ExcelService()
        {
            // EPPlus requiere indicar el contexto de licencia
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
        }

        /// <summary>
        /// Importa basándose en encabezados (fila 1). requiredHeaders define los títulos esperados (case-insensitive).
        /// crearEntidad recibe un diccionario header->valor (string) y debe construir la entidad T.
        /// validarExistenciaAsync debe comprobar asincrónicamente si la entidad ya existe.
        /// Devuelve: cantidad detectada (no necesariamente insertada), lista de errores, entidades parseadas y las filas originales (alineadas con Entidades).
        /// </summary>
        public async Task<(int Importados, List<(int Fila, string Error)> Errores, List<T> Entidades, List<Dictionary<string,string>> Filas)> ImportarDesdeExcel<T>(
            string filePath,
            string[] requiredHeaders,
            Func<Dictionary<string, string>, T> crearEntidad,
            Func<T, Task<bool>> validarExistenciaAsync,
            string sheetName = null) where T : new()
        {
            var importados = 0;
            var errores = new List<(int Fila, string Error)>();
            var entidades = new List<T>();
            var filas = new List<Dictionary<string, string>>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = string.IsNullOrEmpty(sheetName)
                    ? package.Workbook.Worksheets.FirstOrDefault()
                    : package.Workbook.Worksheets.FirstOrDefault(ws => string.Equals(ws.Name, sheetName, StringComparison.OrdinalIgnoreCase));

                if (worksheet == null)
                {
                    errores.Add((0, $"La hoja '{sheetName ?? "primera"}' no existe en el archivo."));
                    return (0, errores, entidades, filas);
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;
                if (rowCount < 2)
                {
                    errores.Add((0, "El archivo no contiene filas de datos."));
                    return (0, errores, entidades, filas);
                }

                // Leer encabezados (fila 1) y mapear título -> índice (1-based), comparando case-insensitive
                var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int c = 1; c <= colCount; c++)
                {
                    var header = worksheet.Cells[1, c].Text?.Trim();
                    if (!string.IsNullOrEmpty(header) && !headerIndex.ContainsKey(header))
                        headerIndex[header] = c;
                }

                // Validar que están los encabezados requeridos
                var missing = requiredHeaders.Where(h => !headerIndex.ContainsKey(h)).ToList();
                if (missing.Any())
                {
                    errores.Add((0, $"Faltan encabezados obligatorios: {string.Join(", ", missing)}"));
                    return (0, errores, entidades, filas);
                }

                for (int row = 2; row <= rowCount; row++) // Empezar desde fila 2 (asumiendo encabezados)
                {
                    try
                    {
                        var datos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var header in headerIndex.Keys)
                        {
                            var col = headerIndex[header];
                            datos[header] = worksheet.Cells[row, col].Text?.Trim() ?? string.Empty;
                        }

                        var entidad = crearEntidad(datos);

                        var existe = await validarExistenciaAsync(entidad);
                        if (!existe)
                        {
                            entidades.Add(entidad);
                            filas.Add(datos); // Guardar fila asociada a la entidad
                            importados++;
                        }
                        else
                        {
                            errores.Add((row, "Registro duplicado"));
                        }
                    }
                    catch (Exception ex)
                    {
                        errores.Add((row, ex.Message));
                    }
                }
            }

            return (importados, errores, entidades, filas);
        }

        public async Task ExportarAExcel<T>(List<T> datos, string filePath, string[] encabezados)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Datos");

                // Encabezados
                for (int i = 0; i < encabezados.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = encabezados[i];
                }

                // Datos: solo los encabezados enviados serán escritos (busca propiedad por nombre)
                var propiedades = typeof(T).GetProperties();
                for (int i = 0; i < datos.Count; i++)
                {
                    for (int j = 0; j < encabezados.Length; j++)
                    {
                        var propName = encabezados[j];
                        var prop = propiedades.FirstOrDefault(p => string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase));
                        var valor = prop != null ? prop.GetValue(datos[i]) : null;
                        worksheet.Cells[i + 2, j + 1].Value = valor?.ToString() ?? "";
                    }
                }

                // Ajustar columnas si hay contenido
                if (worksheet.Dimension != null)
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var fi = new FileInfo(filePath);
                var dir = fi.Directory;
                if (dir != null && !dir.Exists)
                    dir.Create();

                await package.SaveAsAsync(fi);
            }
        }

        /// <summary>
        /// Exporta múltiples hojas en un mismo archivo. Cada entrada del diccionario representa una hoja: key = nombre hoja,
        /// value = (Datos como IEnumerable<object>, Encabezados, Tipo de objetos en Datos).
        /// </summary>
        public async Task ExportarMultiplesAExcel(Dictionary<string, (IEnumerable<object> Datos, string[] Encabezados, Type Tipo)> sheets, string filePath)
        {
            using (var package = new ExcelPackage())
            {
                foreach (var kv in sheets)
                {
                    var sheetName = kv.Key;
                    var datos = kv.Value.Datos?.ToList() ?? new List<object>();
                    var encabezados = kv.Value.Encabezados;
                    var tipo = kv.Value.Tipo;

                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    // Encabezados
                    for (int i = 0; i < encabezados.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = encabezados[i];
                    }

                    var propiedades = tipo.GetProperties();

                    for (int i = 0; i < datos.Count; i++)
                    {
                        for (int j = 0; j < encabezados.Length; j++)
                        {
                            var propName = encabezados[j];
                            var prop = propiedades.FirstOrDefault(p => string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase));
                            var valor = prop != null ? prop.GetValue(datos[i]) : null;
                            worksheet.Cells[i + 2, j + 1].Value = valor?.ToString() ?? "";
                        }
                    }

                    if (worksheet.Dimension != null)
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                var fi = new FileInfo(filePath);
                var dir = fi.Directory;
                if (dir != null && !dir.Exists)
                    dir.Create();

                await package.SaveAsAsync(fi);
            }
        }
    }
}