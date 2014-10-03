using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EapPattern
{
    // http://www.c-sharpcorner.com/uploadfile/nityaprakash/event-based-asynchronous-patterneap/

    // signature d'un gestionnaire de l'événement Methode1Completed
    public delegate void Methode1CompletedEventHandler(object sender, Methode1CompletedEventArgs e);

    public sealed class EapDemo
    {
        /// <summary>
        /// Constructeur
        /// </summary>
        public EapDemo()
        {
            onCompletedDelegate = new SendOrPostCallback(CompletedDelegateFunc);
        }

        // Ce délégué lance la méthode de travail principale en mode asynchrone
        private delegate void WorkerEventHandler(string message, AsyncOperation asyncOp);

        // Délégué utilisé pour lancer CompletedDelegateFunc
        // SendOrPostCallback: Est un délégué (pointeur vers une méthode) qui représente une méthode 
        // à appeler lorsqu'un message doit être distribué à un contexte de synchronisation.
        // Ici la méthode est: CompletedDelegateFunc
        //
        // Ce délégué est principalement utilisé par le contexte de synchronisation: SynchronizationContext.
        // Pour en savoir plus: http://www.codeproject.com/Articles/14265/The-NET-Framework-s-New-SynchronizationContext-Cla
        private SendOrPostCallback onCompletedDelegate;

        // Utilisé pour pouvoir lancer plusieurs fois la méthode asynchrone en mode concurrentiel
        private HybridDictionary tasks = new HybridDictionary();

        // Evénement capturé par le thread principal.
        // Il est levé à chaque fois qu'une instance de tâche asynchrone se terminé (on peut en lancer plusieurs en EP)
        public event Methode1CompletedEventHandler Methode1Completed;

        #region CompletedDelegateFunc (private)
        /// <summary>
        /// Méthode On du pattern standard d'implémentation d'un événement .NET.
        /// Cette méthode lève l'événement Method1Completed
        /// </summary>
        /// <param name="operationState">Instance de Methode1CompletedEventArgs</param>
        private void CompletedDelegateFunc(object operationState)
        {
            Methode1CompletedEventArgs e = operationState as Methode1CompletedEventArgs;

            if (Methode1Completed != null)
            {
                // lève l'événement
                Methode1Completed(this, e);
            }
        }
        #endregion

        #region Methode1
        /// <summary>
        /// Méthode synchrone qui prend du temps
        /// </summary>
        /// <param name="message">Une message à afficher</param>
        public void Methode1(string message)
        {
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(3000);
                Console.WriteLine(message + " " + i.ToString());
            }
        }
        #endregion

        #region Methode1Async
        /// <summary>
        /// Version asynchrone de Methode1
        /// </summary>
        /// <param name="message">Message à afficher</param>
        /// <param name="userState">Valeur unique pour pouvoir éventuellement relancer la tâche en mode concurrentiel</param>
        public void Methode1Async(string message, object userState)
        {
            // AsyncOperationManager: gestionnaire de l'accès concurrentiel à des classes qui supportent l'accès asynchrone de leurs méthodes
            // AsyncOperation peut être utilisé pour suivre la durée d'une tâche asynchrone 
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(userState);

            // Puisque le dictionnaire peut être lu et modifié par plusieurs thread, il est prudent de le locker
            lock (tasks.SyncRoot)
            {
                if (tasks.Contains(userState))
                {
                    throw new ArgumentException("userState doit être unique", "userState");
                }

                tasks[userState] = asyncOp;
            }

            WorkerEventHandler worker = new WorkerEventHandler(Methode1Worker);

            //lance la tâche en mode asynchrone.
            worker.BeginInvoke(message, asyncOp, null, null);
        }
        #endregion

        #region Methode1Worker (private)
        /// <summary>
        /// Méthode qui effectue le vrai boulot
        /// </summary>
        /// <param name="message">Message à afficher</param>
        /// <param name="userState">Valeur unique pour pouvoir éventuellement relancer la tâche en mode concurrentiel</param>
        private void Methode1Worker(string message, AsyncOperation asyncOp)
        {
            // effectue une tâche consommatrice
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(3000);
                Console.WriteLine(message + " " + i.ToString());
            }

            lock (tasks.SyncRoot)
            {
                tasks.Remove(asyncOp.UserSuppliedState);
            }

            // levée de l'événement Methode1Completed
            Methode1CompletedEventArgs e = new Methode1CompletedEventArgs(null, false, asyncOp.UserSuppliedState);
            asyncOp.PostOperationCompleted(onCompletedDelegate, e);
        }
        #endregion
    }
}
