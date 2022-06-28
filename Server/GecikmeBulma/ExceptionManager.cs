using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GecikmeBulma.Trade;
using MySql.Data.MySqlClient;

namespace GecikmeBulma
{
    internal static class ExceptionManager
    {
        internal static void Handle(Action action, string callback="", bool logging=true)
        {
            try
            {
                action.Invoke();
            }
            catch (RecordNotFoundException exception)
            {
                if(logging)
                {
                    string debug = String.Format(" RecordNotFoundException: ({0}) {1}",
                        callback,
                        exception.ToString()
                        );

                    Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                }

                throw;
            }
            catch (MySqlException exception)
            {
                if (logging)
                {
                    string debug = String.Format(" MySqlException: ({0}) {1}",
                        callback,
                        exception.ToString()
                        );

                    Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                } 

                throw;
            }
            catch (Exception exception)
            {
                if (logging)
                {
                    string debug = String.Format(" Exception: ({0}) {1}",
                        callback,
                        exception.ToString()
                        );

                    Utils.SendLog(LoggerService.LoggerType.ERROR, debug);
                }
                    

                throw;
            }
        }
    }

    internal class RecordNotFoundException : Exception
    {
        internal RecordNotFoundException(string message) : base(message)
        {

        }
    }
}
