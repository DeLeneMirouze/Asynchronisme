using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApmPattern
{
    /// <summary>
    /// Démonstration du pattern APM avec les 3 techniques de rendez-vous de base:
    /// * Wait-until-done
    /// * Polling
    /// * Callback (rappel)
    class Program
    {
        // Lire aussi ces articles:
        // http://msdn.microsoft.com/fr-fr/magazine/cc163467.aspx
        // http://msdn.microsoft.com/fr-fr/library/ms228969(v=VS.85)
        // http://codingndesign.com/blog/?p=189

        static void Main(string[] args)
        {
            string fichier = @"Data/TextFile1.txt";
            fichier = Path.Combine(Environment.CurrentDirectory, fichier);

            byte[] buffer;
            IAsyncResult iAsyncResult;
            int numBytes;

            #region Wait until done
            using (FileStream fs = new FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.Asynchronous))
            {
                buffer = new byte[fs.Length];

                Console.WriteLine("Démonstration de la technique par Wait-Until-Done");
                iAsyncResult = fs.BeginRead(buffer, 0, buffer.Length, null, null);

                Console.WriteLine("Ici je fais des trucs important");
                // l'exécution se bloque jusqu'à ce que la méthode asynchrone se termine
                numBytes = fs.EndRead(iAsyncResult);
                Console.WriteLine("1: Lecture terminée, lu {0}  octets (Wait-Until-Done)", numBytes);

                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine();
            }
            #endregion

            #region Polling
            // ce n'est pas la technique la plus utilisée et appréciée
            bool display = false;
            using (FileStream fs = new FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.Asynchronous))
            {
                buffer = new byte[fs.Length];

                Console.WriteLine("Démonstration de la technique par polling");
                iAsyncResult = fs.BeginRead(buffer, 0, buffer.Length, null, null);

                // on ne met pas de thread en attente, mais on vérifie périodiquement que le thread asynchrone soit terminé
                while (!iAsyncResult.IsCompleted)
                {
                    // tant que ce n'est pas terminé
                    // on peut effectuer ici des tâches importantes

                    if (!display)
                    {
                        Console.WriteLine("Tâche importante ici");
                        display = true;
                    }
                }
                numBytes = fs.EndRead(iAsyncResult);
                Console.WriteLine("2: Lu {0}  octets (Polling)", numBytes);

                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine();
            }
            #endregion

            #region Callback (I)
            // la seule méthode qui n'a pas besoin de mettre un Tread en attente
            // c'est la méthode de Rendez-vous que l'on préconise en général
            using (FileStream fs = new FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.Asynchronous))
            {
                buffer = new byte[fs.Length];

                Console.WriteLine("Démonstration de la technique par rappel (callback)");
                iAsyncResult = fs.BeginRead(
                    buffer,
                    0,
                    buffer.Length,
                    new AsyncCallback(Callback), // méthode de rappel
                    fs // la méthode de rappel aura besoin d'accéder à cette ressource, on la passe ici
                    );
            }

            Console.WriteLine("Tâche importante ici");

            // on attend que la tâche asynchrone se termine
            //
            // comprenez bien ce que le ReadLine fait ici. Il est nécessaire UNIQUEMENT parce que nous somme dans la méthode Main
            // si on ne stoppe pas Main en attendant que le thread asynchrone se termine, alors l'application toute entière se terminera
            // et on aura plus grand chose à voir!
            // Dans le cas le plus usuel où la méthode asynchrone est appelée à des endroits plus profonds du code, on a juste besoin de 
            // laisser l'exécution se poursuivre et passer à la suite. Le thread sera juste rendu au pool de thread et rien ne sera bloqué
            Console.ReadLine();
            #endregion

            #region Callback (II)
            // Identique à Callback (I), mais on utilise une méthode anonyme pour passer le délégué
            // mais regardez tout de même les différences, il y en a...

            using (FileStream fs = new FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.Asynchronous))
            {
                buffer = new byte[fs.Length];

                Console.WriteLine("Démonstration de la technique par rappel (callback anonyme)");
                iAsyncResult = fs.BeginRead(
                    buffer,
                    0,
                    buffer.Length,
                    (IAsyncResult ar) =>
                    {
                        int nbOctets = fs.EndRead(ar); // on accède à une variable locale
                        Console.WriteLine("4: Lu: {0}  octets", nbOctets);
                    },
                     null // CETTE FOIS on passe null, car la méthode anonyme peut accéder à toutes les variables locales comme fs
                    );
            }

            Console.WriteLine("Tâche importante ici");

            // on attend que la tâche asynchrone se termine
            //
            // comprenez bien ce que le ReadLine fait ici. Il est nécessaire UNIQUEMENT parce que nous somme dans la méthode Main
            // si on ne stoppe pas Main en attendant que le thread asynchrone se termine, alors l'application toute entière se terminera
            // et on aura plus grand chose à voir!
            // Dans le cas le plus usuel où la méthode asynchrone est appelée à des endroits plus profonds du code, on a juste besoin de 
            // laisser l'exécution se poursuivre et passer à la suite. Le thread sera juste rendu au pool de thread et rien ne sera bloqué
            Console.ReadLine();

            Console.WriteLine("-----------------------------------------------------------");
            Console.WriteLine();
            #endregion

            // IMPORTANT: dans tous les cas, il ne faut pas négliger d'appeler la méthode End
            // Si par exemple une exception se produit durant le traitement asynchrone, c'est au moment du End qu'elle sera levée et pourra donc
            // être analysée et traitée, dans le cas contraire vous risquez des bugs bizarres et difficiles à comprendre et dans certains cas
            // des fuites mémoires


            #region Autre Démo APM
            int total = 1000000000;
            DemoAPM demoApm = new DemoAPM();

            // première implémentation
            demoApm.SommeAsync(total);

            Console.WriteLine("Attendez l'affichage de la somme puis enter");
            Console.ReadLine();

            // deuxième implémentation
            demoApm.SommeAsync2(total);

            Console.WriteLine("Attendez l'affichage de la somme puis enter");
            Console.ReadLine();

            // Troisième implémentation (APM transformé en Task)
            //var t = demoApm.GetSumAsTask(total);
            var t = demoApm.GetSumAsTask2(total);
            Console.WriteLine("On fait des trucs pendant que l'opération asynchrone s'exécute");
            t.Wait();

            Console.WriteLine("Résultat: {0}", t.Result);
            #endregion
        }

        #region Callback (private)
        /// <summary>
        /// Méthode appelée une fois terminé l'appel asynchrone
        /// </summary>
        /// <param name="ar"></param>
        public static void Callback(IAsyncResult ar)
        {
            FileStream fs = (FileStream)ar.AsyncState;
            int numBytes = fs.EndRead(ar); // on appelle bien EndRead, c'est important

            Console.WriteLine("3: Lu: {0}  octets", numBytes);

            // on n'est pas obligé d'utiliser une méthode de callback, on pourrait appeler EndRead() directement dans le code
            // au moment où l'on a  besoin du résultat de la méthode asynchrone. 
            // EndRead() attend que la méthode soit terminée, on passe donc de l'asynchrone au synchrone
        }
        #endregion
    }
}
