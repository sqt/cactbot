using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cactbot
{
    public class BrowserBindings
    {
        const int MaxLogLinesRetained = 2000;
        Queue<string> logLines = new Queue<string>();

        FFXIVMemory memory = null;
        
        List<Combatant> combatants = new List<Combatant>();
        List<AggroEntry> aggros = new List<AggroEntry>();
        List<EnmityEntry> enmities = new List<EnmityEntry>();

        public BrowserBindings()
        {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.OnLogLineRead += OnLogLineRead;
        }

        ~BrowserBindings()
        {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.OnLogLineRead -= OnLogLineRead;
        }

        public string CurrentZone()
        {
            return FFXIV_ACT_Plugin.ACTWrapper.CurrentZone;
        }

        public bool InCombat()
        {
            return FFXIV_ACT_Plugin.ACTWrapper.InCombat;
        }

        public void TextToSpeech(string speechText)
        {
            // FIXME: This appears to be synchronous.  Maybe kick the task to a thread?
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.TTS(speechText);
        }

        public void UpdateCombatants()
        {
            // FIXME: Don't bother doing this on every call to GetCombatant
            // Maybe the presence of additional loglines being read could
            // be the dirty flag here.
            combatants = FFXIVPluginHelper.GetCombatantList();
        }

        public List<AggroEntry> GetAggroList()
        {
            if (memory == null || memory.validateProcess() == false)
                memory = new FFXIVMemory(FFXIVPluginHelper.GetFFXIVProcess);

            if (memory == null || memory.validateProcess() == false)
                return null;

            return memory.GetAggroList();
        }

        public List<EnmityEntry> GetEnmityList()
        {
            if (memory == null || memory.validateProcess() == false)
                memory = new FFXIVMemory(FFXIVPluginHelper.GetFFXIVProcess);

            if (memory == null || memory.validateProcess() == false)
                return null;

            return memory.GetEnmityEntryList();
        }

        public void UpdateAggroList()
        {
            aggros = GetAggroList();
        }

        public void UpdateEnmityList()
        {
            enmities = GetEnmityList();
        }

        public int NumAggros()
        {
            if (aggros == null)
                return 0;
            return aggros.Count;
        }

        public AggroEntry GetAggro(int idx)
        {
            if (aggros == null)
                return null;
            if (idx < 0 || idx >= aggros.Count)
                return null;
            return aggros[idx];
        }

        public int NumEnmity()
        {
            if (enmities == null)
                return 0;
            return enmities.Count;
        }

        public EnmityEntry GetEnmity(int idx)
        {
            if (enmities == null)
                return null;
            if (idx < 0 || idx >= enmities.Count)
                return null;
            return enmities[idx];
        }

        public int NumCombatants()
        {
            if (combatants == null)
                return 0;
            return combatants.Count;
        }

        public Combatant GetCombatant(int idx)
        {
            if (combatants == null)
                return null;
            if (idx < 0 || idx >= combatants.Count)
                return null;
            return combatants[idx];
        }

        private static bool CombatantIsMob(Combatant c)
        {
            // Not a pet, not a player.
            return c.OwnerID == 0 && c.Job == 0;
        }

        public Combatant GetMobByName(string name)
        {
            Combatant found = null;
            foreach (Combatant c in combatants) {
                if (!CombatantIsMob(c))
                    continue;
                if (c.Name != name)
                    continue;
                // Multiple mobs with the same name.
                if (found != null)
                    return null;
                found = c;
            }
            return found;
        }

        // FIXME: Add GetPartyIds.
        // FIXME: Add GetMobIdsByName.

        // Provided as a helper function as GetCombatant(0) is a total hack and
        // maybe something more reliable can be found.
        public Combatant GetPlayer()
        {
            return GetCombatant(0);
        }

        // FIXME: javascript should probably register for loglines it cares about?
        // This is a hella awkward interface.
        public bool HasLogLines()
        {
            return logLines.Count > 0;
        }

        public string NextLogLine()
        {
            return logLines.Dequeue();
        }

        private void OnLogLineRead(bool isImport, LogLineEventArgs args)
        {
            if (isImport)
                return;
            Debug.Assert(args.logLine != null);
            // FIXME: Remove this check once the source of the null log lines is found.
            if (args.logLine != null)
            {
                logLines.Enqueue(args.logLine);
            }

            // FIXME: expose this error to the browser.
            if (logLines.Count > MaxLogLinesRetained)
                logLines.Dequeue();
        }
    }
}
