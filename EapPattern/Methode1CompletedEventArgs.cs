using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EapPattern
{
    /// <summary>
    /// Arguments de l'événement Methode1Completed
    /// </summary>
    public sealed class Methode1CompletedEventArgs : AsyncCompletedEventArgs
    {
        public Methode1CompletedEventArgs(Exception ex, bool canceled, object userState)
            : base(ex, canceled, userState)
        {
            // mettre ici une propriété en lecture seule pour le type de retour de la méthode (a)synchrone

        }
    }
}
