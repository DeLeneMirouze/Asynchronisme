using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EapPattern
{
    /// <summary>
    /// Démonstration du pattern Eap.
    /// On l'appelle parfois Event-based pattern
    /// 
    /// On utilise soit une classe où Async est déjà implémentés, soit une implémentation personnalisée.
    /// Nous avons choisit cette dernière option.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            EapDemo demo = new EapDemo();
            demo.Methode1Completed += new Methode1CompletedEventHandler(demo_Method1Completed);

            // EAP est capable de lancer concurrentiellement plusieurs fois la tâche asynchrone
            demo.Methode1Async("Async 1", "userstate: Async1");
            demo.Methode1Async("Async 2", "userstate: Async2");
            //demo.Methode1("Synchrone");

            Console.WriteLine("It's all done folks!");
            Console.ReadLine();
        }

        #region demo_Method1Completed (private)
        /// <summary>
        /// Action lorsque l'une des instances de tâche asynchrone est terminée
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void demo_Method1Completed(object sender, Methode1CompletedEventArgs e)
        {
            Console.WriteLine("{0} terminé", e.UserState);
        }
        #endregion
    }
}
