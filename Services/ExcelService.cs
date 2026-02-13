using DocumentalManager.Models;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DocumentalManager.Services
{
    public class ExcelService
    {
        public async Task<(int Importados, List<(int Fila, string Error)> Errores)> ImportarDesdeExcel<T>(
            string filePath,
            Dictionary<string, int> columnas,
            System.Func<Dictionary<string, string>, T> crearEntidad,
            System.Func<T, bool> validarExistencia) where T : new()
        {
            var importados = 0;
            var errores = new List<(int Fila, string Error)>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    for (int row = 2; row <= rowCount; row++) // Empezar desde fila 2 (asumiendo encabezados)
                    {
                        try
                        {
                            var datos = new Dictionary<string, string>();
                            foreach (var col in columnas)
                            {
                                var valor = worksheet.Cells[row, col.Value].Text;
                                datos[col.Key] = valor;
                            }

                            var entidad = crearEntidad(datos);

                            if (!validarExistencia(entidad))
                            {
                                // Insertar entidad (esto se manejará en el ViewModel)
                                importados++;
                            }
                            else
                            {
                                errores.Add((row, "Registro duplicado"));
                            }
                        }
                        catch (System.Exception ex)
                        {
                            errores.Add((row, ex.Message));
                        }
                    }
                }
            }

            return (importados, errores);
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

                // Datos
                var propiedades = typeof(T).GetProperties();
                for (int i = 0; i < datos.Count; i++)
                {
                    for (int j = 0; j < propiedades.Length; j++)
                    {
                        var valor = propiedades[j].GetValue(datos[i]);
                        worksheet.Cells[i + 2, j + 1].Value = valor?.ToString() ?? "";
                    }
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                await package.SaveAsAsync(new FileInfo(filePath));
            }
        }
    }
}