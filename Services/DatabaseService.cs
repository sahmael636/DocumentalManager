using DocumentalManager.Models;
using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System;

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

        public Task<T> GetByIdAsync<T>(string id) where T : new()
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

        public async Task<bool> HasRelatedRecordsAsync(string tableName, string parentId)
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

        /// <summary>
        /// Borra en cascada: primero borra todos los hijos (recursivo) y luego la entidad padre.
        /// id ahora es string (GUID)
        /// </summary>
        public async Task DeleteCascadeAsync(string tableName, string id)
        {
            switch (tableName)
            {
                case "Fondo":
                    var subfondos = await _database.Table<Subfondo>().Where(s => s.FondoId == id).ToListAsync();
                    foreach (var s in subfondos)
                    {
                        await DeleteCascadeAsync("Subfondo", s.Id);
                    }
                    var fondo = await _database.FindAsync<Fondo>(id);
                    if (fondo != null) await _database.DeleteAsync(fondo);
                    break;

                case "Subfondo":
                    var unidades = await _database.Table<UnidadAdministrativa>().Where(u => u.SubfondoId == id).ToListAsync();
                    foreach (var u in unidades)
                    {
                        await DeleteCascadeAsync("UnidadAdministrativa", u.Id);
                    }
                    var subfondo = await _database.FindAsync<Subfondo>(id);
                    if (subfondo != null) await _database.DeleteAsync(subfondo);
                    break;

                case "UnidadAdministrativa":
                    var oficinas = await _database.Table<OficinaProductora>().Where(o => o.UnidadAdministrativaId == id).ToListAsync();
                    foreach (var o in oficinas)
                    {
                        await DeleteCascadeAsync("OficinaProductora", o.Id);
                    }
                    var unidad = await _database.FindAsync<UnidadAdministrativa>(id);
                    if (unidad != null) await _database.DeleteAsync(unidad);
                    break;

                case "OficinaProductora":
                    var series = await _database.Table<Serie>().Where(s => s.OficinaProductoraId == id).ToListAsync();
                    foreach (var s in series)
                    {
                        await DeleteCascadeAsync("Serie", s.Id);
                    }
                    var oficina = await _database.FindAsync<OficinaProductora>(id);
                    if (oficina != null) await _database.DeleteAsync(oficina);
                    break;

                case "Serie":
                    var subseries = await _database.Table<Subserie>().Where(s => s.SerieId == id).ToListAsync();
                    foreach (var ss in subseries)
                    {
                        await DeleteCascadeAsync("Subserie", ss.Id);
                    }
                    var serie = await _database.FindAsync<Serie>(id);
                    if (serie != null) await _database.DeleteAsync(serie);
                    break;

                case "Subserie":
                    var tipos = await _database.Table<TipoDocumental>().Where(t => t.SubserieId == id).ToListAsync();
                    foreach (var t in tipos)
                    {
                        await DeleteCascadeAsync("TipoDocumental", t.Id);
                    }
                    var subserie = await _database.FindAsync<Subserie>(id);
                    if (subserie != null) await _database.DeleteAsync(subserie);
                    break;

                case "TipoDocumental":
                    var tipo = await _database.FindAsync<TipoDocumental>(id);
                    if (tipo != null) await _database.DeleteAsync(tipo);
                    break;
            }
        }

        // Métodos de búsqueda
        public async Task<List<BusquedaResultado>> BuscarPorTexto(string texto)
        {
            var resultados = new List<BusquedaResultado>();
            if (string.IsNullOrWhiteSpace(texto))
                return resultados;

            var q = texto.Trim();

            // 1) Buscar por TipoDocumental (resultado más específico)
            var tipos = await _database.Table<TipoDocumental>().ToListAsync();
            var tiposMatch = tipos.Where(t =>
                (!string.IsNullOrEmpty(t.Nombre) && t.Nombre.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(t.Codigo) && t.Codigo.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            foreach (var tipo in tiposMatch)
            {
                var subserie = await _database.FindAsync<Subserie>(tipo.SubserieId);
                var serie = subserie != null ? await _database.FindAsync<Serie>(subserie.SerieId) : null;
                var oficina = serie != null ? await _database.FindAsync<OficinaProductora>(serie.OficinaProductoraId) : null;
                var unidad = oficina != null ? await _database.FindAsync<UnidadAdministrativa>(oficina.UnidadAdministrativaId) : null;
                var subfondo = unidad != null ? await _database.FindAsync<Subfondo>(unidad.SubfondoId) : null;
                var fondo = subfondo != null ? await _database.FindAsync<Fondo>(subfondo.FondoId) : null;

                var codigoCompleto = $"{fondo?.Codigo}.{subfondo?.Codigo}.{unidad?.Codigo}.{oficina?.Codigo}.{serie?.Codigo}.{subserie?.Codigo}.{tipo.Codigo}";

                resultados.Add(new BusquedaResultado
                {
                    Fondo = fondo?.Nombre ?? "",
                    Subfondo = subfondo?.Nombre ?? "",
                    UnidadAdministrativa = unidad?.Nombre ?? "",
                    OficinaProductora = oficina?.Nombre ?? "",
                    Serie = serie?.Nombre ?? "",
                    Subserie = subserie?.Nombre ?? "",
                    TipoDocumental = tipo.Nombre,
                    CodigoCompleto = codigoCompleto,
                    AG = subserie?.AG ?? 0,
                    AC = subserie?.AC ?? 0,
                    Papel = subserie?.P ?? false,
                    Electronico = subserie?.EL ?? false,
                    FormatoDigital = subserie?.FormatoDigital ?? "",
                    ConservacionTotal = subserie?.CT ?? false,
                    Eliminacion = subserie?.E ?? false,
                    MediosTecnologicos = subserie?.MT ?? false,
                    Seleccion = subserie?.S ?? false,
                    Procedimiento = subserie?.Procedimiento ?? ""
                });
            }

            // 2) Buscar por Subserie (si no hay tipo, igual mostrar la subserie)
            var subseries = await _database.Table<Subserie>().ToListAsync();
            var subseriesMatch = subseries.Where(s =>
                (!string.IsNullOrEmpty(s.Nombre) && s.Nombre.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(s.Codigo) && s.Codigo.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            foreach (var subserie in subseriesMatch)
            {
                // Evitar duplicados si ya añadimos resultados que contengan esta subserie (por tipos)
                if (resultados.Any(r => r.Subserie == subserie.Nombre)) continue;

                var serie = await _database.FindAsync<Serie>(subserie.SerieId);
                var oficina = serie != null ? await _database.FindAsync<OficinaProductora>(serie.OficinaProductoraId) : null;
                var unidad = oficina != null ? await _database.FindAsync<UnidadAdministrativa>(oficina.UnidadAdministrativaId) : null;
                var subfondo = unidad != null ? await _database.FindAsync<Subfondo>(unidad.SubfondoId) : null;
                var fondo = subfondo != null ? await _database.FindAsync<Fondo>(subfondo.FondoId) : null;

                var codigoCompleto = $"{fondo?.Codigo}.{subfondo?.Codigo}.{unidad?.Codigo}.{oficina?.Codigo}.{serie?.Codigo}.{subserie.Codigo}";

                resultados.Add(new BusquedaResultado
                {
                    Fondo = fondo?.Nombre ?? "",
                    Subfondo = subfondo?.Nombre ?? "",
                    UnidadAdministrativa = unidad?.Nombre ?? "",
                    OficinaProductora = oficina?.Nombre ?? "",
                    Serie = serie?.Nombre ?? "",
                    Subserie = subserie.Nombre,
                    TipoDocumental = "", // no hay tipo asociado en esta coincidencia
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

            // 3) (Opcional) Buscar en Serie / Oficina / Unidad / Subfondo / Fondo y agregar entradas representativas
            // Por brevedad añadimos búsqueda en Serie (puedes replicar el patrón para niveles superiores)
            var series = await _database.Table<Serie>().ToListAsync();
            var seriesMatch = series.Where(s =>
                (!string.IsNullOrEmpty(s.Nombre) && s.Nombre.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(s.Codigo) && s.Codigo.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            foreach (var serie in seriesMatch)
            {
                if (resultados.Any(r => r.Serie == serie.Nombre)) continue;

                var oficina = await _database.FindAsync<OficinaProductora>(serie.OficinaProductoraId);
                var unidad = oficina != null ? await _database.FindAsync<UnidadAdministrativa>(oficina.UnidadAdministrativaId) : null;
                var subfondo = unidad != null ? await _database.FindAsync<Subfondo>(unidad.SubfondoId) : null;
                var fondo = subfondo != null ? await _database.FindAsync<Fondo>(subfondo.FondoId) : null;

                var codigoCompleto = $"{fondo?.Codigo}.{subfondo?.Codigo}.{unidad?.Codigo}.{oficina?.Codigo}.{serie.Codigo}";

                resultados.Add(new BusquedaResultado
                {
                    Fondo = fondo?.Nombre ?? "",
                    Subfondo = subfondo?.Nombre ?? "",
                    UnidadAdministrativa = unidad?.Nombre ?? "",
                    OficinaProductora = oficina?.Nombre ?? "",
                    Serie = serie.Nombre,
                    Subserie = "",
                    TipoDocumental = "",
                    CodigoCompleto = codigoCompleto
                });
            }

            // Puedes extender el patrón (Oficina, Unidad, Subfondo, Fondo) si lo deseas.

            return resultados;
        }

        // Método adicional
        public async Task<bool> ExistsByCodigoAsync<T>(string codigo) where T : BaseEntity, new()
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            var codigoNormalized = codigo.Trim();
            var count = await _database.Table<T>().Where(x => x.Codigo == codigoNormalized).CountAsync();
            return count > 0;
        }
    }
}