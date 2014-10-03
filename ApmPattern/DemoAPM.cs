using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApmPattern
{
    /// <summary>
    /// Exemple d'implémentation d'APM.
    /// 
    /// Nous fournissons deux exemples d'implémentation pour bien montrer la différence entre un pattern et une implémentation
    /// particulière
    /// </summary>
    /// <remarks>
    /// Quelques bonnes lectures:
    /// 
    /// http://www.codeproject.com/Articles/37244/Understanding-the-Asynchronous-Programming-Model
    /// Cette page est extrait d'un article en plusieurs partie sur tout ce qui touche au threading:
    /// http://www.yoda.arachsys.com/csharp/threads/threadpool.shtml
    /// 
    /// </remarks>
    sealed class DemoAPM
    {
        #region Somme (synchrone)
        /// <summary>
        /// Calcule (de façon très inefficace) la somme des n premiers entiers de façon synchrone
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public long Somme(int n)
        {
            long retour = 0;
            checked // une exception sera levée si une erreur de débordement se produit
            {
                for (int i = 1; i <= n; i++)
                {
                    retour += i;
                }
            }

            return retour;
        }
        #endregion

        #region Première version: utilisation de delegate
        /* Un délégate (délégué) est simplement un pointeur vers une méthode.
         * 
         * Lorsque l'on créée un delégué, le compilateur créée aussi 3 méthodes: Invoke, BeginInvoke et EndInvoke.
         * Invoke lance une exécution synchrone de la méthode, tandis que Begin et End sont utilisés pour les invocation asynchrones.
         * Comme on va le voir c'est très simple.
         * 
         */

        internal delegate long SommeDelegate(int n);

        /// <summary>
        /// version asynchrone de Somme
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public IAsyncResult SommeAsync(int n)
        {
            // on implémente une méthode de rendez-vous par callback (ComputeIsDone)
            SommeDelegate sumdelegate = Somme;
            // la méthode est mise dans une queue du pool de thread en appellant en interne ThreadPool.QueueUserworkItem
            return sumdelegate.BeginInvoke(n, ComputeIsDone, sumdelegate);

            // avec des versions de C# plus anciennes on devait écrire
            //var rappel = new AsyncCallback(ComputeIsDone);
            //return sumdelegate.BeginInvoke(n, rappel, sumdelegate);
        }

        /// <summary>
        /// Fonction de callback
        /// </summary>
        /// <param name="iar"></param>
        private void ComputeIsDone(IAsyncResult iar)
        {
            SommeDelegate sumdelegate = (SommeDelegate)iar.AsyncState;

            try
            {
                // le principal intérêt de cette fonction est ici: appeler la méthode End automatiquement sans avoir à s'en soucier
                // c'est utile si on veut lancer la méthode asynchrone en mode "fire and forget"
                long somme = sumdelegate.EndInvoke(iar);

                Console.WriteLine("Somme: {0}", somme);
            }
            catch (OverflowException)
            {
                // l'appel de EndInvoke a automatiquement levé l'exception, on l'intercepte ici
                // on pourrait aussi faire un throw
                Console.WriteLine("Erreur de débordement");
            }
        }
        #endregion

        #region Deuxième version: utilisation du ThreadPool
        /* Il est le plus souvent préférable d'utiliser le ThreadPool plutôt que d'instancier un Thread.
         * Par défaut le pool de thread est de 25 thread par processeur. On doit toujours veiller à ne pas modifier
         * Les états du thread du pool puisqu'ils sont recyclés. Si ce n'est pas possible, le mieux est alors d'instancier un Thread.
         * Les threads du pool se caractérisent par leur propriété IsThreadPoolThread à True. ce sont des threads de background.
         */

        /// <summary>
        /// version asynchrone de Somme
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public void SommeAsync2(int n)
        {
            // QueueUserWorkItem attend une fonction dont la signature est: void Toto(object param)
            // d'où l'introduction de SommeTp qui encapsule Somme
            ThreadPool.QueueUserWorkItem(SommeTp, n);

            // On rencontre aussi dans d'anciennes versions de C# le code suivant:
            //var waitCallback = new WaitCallback(SommeTp);
            //ThreadPool.QueueUserWorkItem(waitCallback, n);
        }

        private void SommeTp(object parametre)
        {
            int valeur = Convert.ToInt32(parametre);
            long retour = Somme(valeur);
            Console.WriteLine("Somme: {0}", retour);
        }
        #endregion

        /* On peut se demander la différence entre la méthode par delegate et celle par pool de thread. 
         * Chris Brumme (qui à l'époque travaillait dans l'équipe qui développe la CLR) répond à cette question ici:
         * http://blogs.msdn.com/b/cbrumme/archive/2003/07/14/51495.aspx
         * Pour info Rotor est une implémentation alternative que MS fournissait jadis
         * 
         * C'est technique, mais pour faire simple: BeginInvoke/EndInvoke est plus lent que ThreadPool!
         * Ceci étant il est difficile de gérer les exceptions avec le pool de thread. Depuis .Net 2, une exception terminera l'AppDomain.
         * 
         * On pourrait alors se demander pourquoi ne pas utiliser un pool de thread plus large que 25?
         * Une réponse détaillée ici:
         * http://stackoverflow.com/questions/9453560/why-use-async-requests-instead-of-using-a-larger-threadpool
         */

        #region GetSumAsTask
        /// <summary>
        /// On encapsule APM dans une Task.
        /// Démo de deux façons différentes de le faire
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public Task<long> GetSumAsTask(int n)
        {
            // cet exemple montre un des intérêts de TaskCompletionSource qui est sa capacité de transformer en Task
            // n'importe quel code et de créer des scénarios très sophistiqués de Task

            SommeDelegate sumdelegate = Somme;

            // ce code n'est pas forcément complet, par exemple on ne gère pas l'annulation de la tâche
            var tcs = new TaskCompletionSource<long>();
            sumdelegate.BeginInvoke(n, iar =>
            {
                try
                {
                    long somme = sumdelegate.EndInvoke(iar);
                    tcs.SetResult(somme);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        public Task<long> GetSumAsTask2(int n)
        {
            // version moins verbeuse de GetSumAsTask
            // on utilise FromAsync qui encapsule tout le travail précédent
            // mais on ne voit pas le TaskCompletionSource dans ce cas

            SommeDelegate sumdelegate = Somme;

            Task<long> task = Task<long>.Factory.FromAsync(
                sumdelegate.BeginInvoke,
                sumdelegate.EndInvoke,
                n,
                null);

            return task;
        }
        #endregion
    }
}
