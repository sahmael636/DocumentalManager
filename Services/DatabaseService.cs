using DocumentalManager.Models;
using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DocumentalManager.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "documental.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            InitializeTables();
        }

        private async void InitializeTables()
        {
            await _database.CreateTableAsync<Fondo>();
            await _database.CreateTableAsync<Subfondo>();
            await _database.CreateTableAsync<UnidadAdministrativa>();
            await _database.CreateTableAsync<OficinaProductora>();
            await _database.CreateTableAsync<Serie>();
            await _database.CreateTableAsync<Subserie>();
            await _database.CreateTableAsync<TipoDocumental>();
        }

        // Métodos genéricos
        public Task<List<T>> GetAllAsync<T>() where T : new()
        {
            return _database.Table<T>().ToListAsync();
        }

        public Task<T> GetByIdAsync<T>(int id) where T : new()
        {
            return _database.FindAsync<T>(id);
        }

        public Task<int> InsertAsync<T>(T item)
        {
            return _database.InsertAsync(item);
        }

        public Task<int> UpdateAsync<T>(T item)
        {
            return _database.UpdateAsync(item);
        }

        public Task<int> DeleteAsync<T>(T item)
        {
            return _database.DeleteAsync(item);
        }

        public async Task<bool> HasRelatedRecordsAsync(string tableName, int parentId)
        {
            var result = false;

            switch (tableName)
            {
                case "Fondo":
                    var subfondos = await _database.Table<Subfondo>().Where(s => s.FondoId == parentId).CountAsync();
                    result = subfondos > 0;
                    break;
                case "Subfondo":
                    var unidades = await _database.Table<UnidadAdministrativa>().Where(u => u.SubfondoId == parentId).CountAsync();
                    result = unidades > 0;
                    break;
                case "UnidadAdministrativa":
                    var oficinas = await _database.Table<OficinaProductora>().Where(o => o.UnidadAdministrativaId == parentId).CountAsync();
                    result = oficinas > 0;
                    break;
                case "OficinaProductora":
                    var series = await _database.Table<Serie>().Where(s => s.OficinaProductoraId == parentId).CountAsync();
                    result = series > 0;
                    break;
                case "Serie":
                    var subseries = await _database.Table<Subserie>().Where(s => s.SerieId == parentId).CountAsync();
                    result = subseries > 0;
                    break;
                case "Subserie":
                    var tipos = await _database.Table<TipoDocumental>().Where(t => t.SubserieId == parentId).CountAsync();
                    result = tipos > 0;
                    break;
            }

            return result;
        }

        // Métodos de búsqueda
        public async Task<List<BusquedaResultado>> BuscarPorTexto(string texto)
        {
            var resultados = new List<BusquedaResultado>();

            var tiposDocumentales = await _database.Table<TipoDocumental>()
                .Where(t => t.Nombre.Contains(texto) || t.Codigo.Contains(texto))
                .ToListAsync();

            foreach (var tipo in tiposDocumentales)
            {
                var subserie = await _database.FindAsync<Subserie>(tipo.SubserieId);
                if (subserie != null)
                {
                    var serie = await _database.FindAsync<Serie>(subserie.SerieId);
                    var oficina = serie != null ? await _database.FindAsync<OficinaProductora>(serie.OficinaProductoraId) : null;
                    var unidad = oficina != null ? await _database.FindAsync<UnidadAdministrativa>(oficina.UnidadAdministrativaId) : null;
                    var subfondo = unidad != null ? await _database.FindAsync<Subfondo>(unidad.SubfondoId) : null;
                    var fondo = subfondo != null ? await _database.FindAsync<Fondo>(subfondo.FondoId) : null;

                    var codigoCompleto = $"{fondo?.Codigo}.{subfondo?.Codigo}.{unidad?.Codigo}.{oficina?.Codigo}.{serie?.Codigo}.{subserie.Codigo}.{tipo.Codigo}";

                    resultados.Add(new BusquedaResultado
                    {
                        Fondo = fondo?.Nombre ?? "",
                        Subfondo = subfondo?.Nombre ?? "",
                        UnidadAdministrativa = unidad?.Nombre ?? "",
                        OficinaProductora = oficina?.Nombre ?? "",
                        Serie = serie?.Nombre ?? "",
                        Subserie = subserie.Nombre,
                        TipoDocumental = tipo.Nombre,
                        CodigoCompleto = codigoCompleto,
                        AG = subserie.AG,
                        AC = subserie.AC,
                        Papel = subserie.P,
                        Electronico = subserie.EL,
                        FormatoDigital = subserie.FormatoDigital,
                        ConservacionTotal = subserie.CT,
                        Eliminacion = subserie.E,
                        MediosTecnologicos = subserie.MT,
                        Seleccion = subserie.S,
                        Procedimiento = subserie.Procedimiento
                    });
                }
            }

            return resultados;
        }
    }
}