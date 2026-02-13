using DocumentalManager.Views;
namespace DocumentalManager
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("ListaPage", typeof(ListaPage));
            Routing.RegisterRoute("FormularioPage", typeof(FormularioPage));
        }
    }
}
