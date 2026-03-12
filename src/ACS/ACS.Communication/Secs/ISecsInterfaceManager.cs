using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Secs
{
    public interface ISecsInterfaceManager
    {
        //사용하지 않지만 임의로 만듬.
        void load(string paramstring);

        //void loadSecsInterface(Secs paramSecs);


        void loadSecsInterface(string paramstring);


        void loadSecsInterface(string paramstring1, string paramstring2);

       // void loadSecsInterface(string paramstring, AbstractSecsDriver paramAbstractSecsDriver);

        void unload();

        void unloadSecsInterface(string paramstring);

        void synchronizeAll(string paramstring);

        void synchronizeSecsInterfaces(string paramstring);

        void synchronizeSecsInterface(string paramstring1, string paramstring2);

        void reloadAll(string paramstring);

        void reloadSecsInterface(string paramstring1, string paramstring2);

        void startAll();

        int startSecsInterface(string paramstring);

        //int startSecsInterface(SecsControllable paramSecsControllable);

        void stopAll();

        bool stopSecsInterface(string paramstring);

        //bool stopSecsInterface(SecsControllable paramSecsControllable);

        void restartAll();

        void restartSecsInterface(string paramstring);

        bool isConnected(string paramstring);

        //bool isConnected(SecsControllable paramSecsControllable);

        Dictionary<string, string> getSecsInterfaces();

        int getSecsInterfaceCount();

        //SecsControllable getSecsInterface(string paramstring);

       // AbstractSecsDriver getSecsDriver(string paramstring);

       // AbstractSecsDriver getSecsDriverByMachine(string paramstring);

        void updateSecsInterface(string paramstring);

        void displayAll();

        string getSecsInterfaceNames();

       // void addSecs(Secs paramSecs);

       // void updateSecs(Secs paramSecs);

       // void removeSecs(Secs paramSecs);

        int getSecsCount();

        int updateSecsState(string paramstring1, string paramstring2, string paramstring3);

       // int updateSecsState(Secs paramSecs);

       // int updateSecsState(AbstractSecsDriver paramAbstractSecsDriver, string paramstring);

        string getConnectionState(string paramstring);

        string getControlState(string paramstring);

        string getTscState(string paramstring);

        string getReconcileState(string paramstring);

        /**
         * @deprecated
         */
        //Secs getSecs(string paramstring);

        //Secs getSecs(string paramstring1, string paramstring2);

        //List getSecses();

        //List getSecses(string paramstring);

        //List getSecsByMachine(string paramstring);

        //List getSecsByMachineType(string paramstring);

        //List getSecsByMachineAndIdentity(string paramstring1, string paramstring2);

        //List getSecsByMachineTypeAndIdentity(string paramstring1, string paramstring2);

        //List getSecsByApplicationName(string paramstring);

        //List getSecsByMachineAndApplicationName(string paramstring);

        //List getSecsByMachineAndApplicationName(string paramstring1, string paramstring2);
    }
}
