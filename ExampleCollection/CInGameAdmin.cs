/*  Copyright 2010 Geoffrey 'Phogue' Green

	This file is part of BFBC2 PRoCon.

	BFBC2 PRoCon is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	BFBC2 PRoCon is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;
using PRoCon.Core.Plugin.Commands;

namespace PRoConEvents
{
    public class CInGameAdmin : PRoConPluginAPI, IPRoConPluginInterface
    {
        private PlayerInformationDictionary m_dicPlayers;

        private CMap m_currentMap;
        private List<string> m_squadNames;

        private string m_strPrivatePrefix;
        private string m_strAdminsPrefix;
        private string m_strPublicPrefix;

        private string m_strKickCommand;
        private string m_strKillCommand;
        private string m_strNukeCommand;
        private string m_strMoveCommand;
        private string m_strForceMoveCommand;
        private string m_strTemporaryBanCommand;
        private string m_strPermanentBanCommand;
        private string m_strSayCommand;
        private string m_strPlayerSayCommand;
        private string m_strYellCommand;
        private string m_strPlayerYellCommand;
        private string m_strRestartLevelCommand;
        private string m_strNextLevelCommand;
        private string m_strEndLevelCommand;
        private string m_strExecuteConfigCommand;
        private string m_strConfirmCommand;
        private string m_strCancelCommand;

        private string m_strBanTypeOption;
        private enumBoolYesNo m_enBanIncAdmin;

        private int m_iShowMessageLength;
        private string m_strShowMessageLength;

        // Status
        private string m_strServerGameType;

        private string m_strGameMod;
        private string m_strServerVersion;
        private string m_strPRoConVersion;

        private int m_iYellDivider;

        private bool m_blHasYellDuration;
        private bool m_isPluginEnabled;

        public CInGameAdmin()
        {
            this.m_isPluginEnabled = false;
            this.m_squadNames = new List<string>();

            this.m_dicPlayers = new PlayerInformationDictionary();
            this.m_dicPlayers.SendResponse += new PlayerInformationDictionary.SendResponseHandler(m_dicPlayers_SendResponse);
            this.m_dicPlayers.ExecuteCommand += new PlayerInformationDictionary.ExecuteCommandHandler(m_dicPlayers_ExecuteCommand);
            this.m_dicPlayers.QueueYellingResponse += new PlayerInformationDictionary.QueueYellingResponseHandler(m_dicPlayers_QueueYellingResponse);
            this.m_dicPlayers.QueueResponse += new PlayerInformationDictionary.QueueResponseHandler(m_dicPlayers_QueueResponse);
            this.m_dicPlayers.SendYellingResponse += new PlayerInformationDictionary.SendYellingResponseHandler(m_dicPlayers_SendYellingResponse);

            this.m_strPrivatePrefix = "@";
            this.m_strAdminsPrefix = "#";
            this.m_strPublicPrefix = "!";

            this.m_strKickCommand = "kick";
            this.m_strKillCommand = "kill";
            this.m_strNukeCommand = "nuke";
            this.m_strMoveCommand = "move";
            this.m_strForceMoveCommand = "fmove";
            this.m_strTemporaryBanCommand = "tban";
            this.m_strPermanentBanCommand = "ban";
            this.m_strSayCommand = "say";
            this.m_strPlayerSayCommand = "psay";
            this.m_strYellCommand = "yell";
            this.m_strPlayerYellCommand = "pyell";

            this.m_strRestartLevelCommand = "restart";
            this.m_strNextLevelCommand = "nextlevel";
            this.m_strEndLevelCommand = "endround";
            this.m_strConfirmCommand = "yes";
            this.m_strCancelCommand = "cancel";
            this.m_strExecuteConfigCommand = "exec";

            this.m_strBanTypeOption = "Frostbite - Name";
            this.m_enBanIncAdmin = enumBoolYesNo.No;

            this.m_iShowMessageLength = 8000;
            this.m_strShowMessageLength = "8000";

            this.m_strServerGameType = "none";
            this.m_iYellDivider = 1;
            this.m_blHasYellDuration = true;
        }

        public string GetPluginName()
        {
            return "In-Game Admin";
        }

        public string GetPluginVersion()
        {
            return "3.3.1.0";
        }

        public string GetPluginAuthor()
        {
            return "Phogue";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>
<p>Exposes some basic text based commands to players with accounts and necessary privileges</p>

<h2>Command Response Scopes (default)</h2>
	<blockquote><h4>!</h4>Responses will be displayed to everyone in the server.  Everyone will see ""Phogue has been kicked for team killing"".</blockquote>
	<blockquote><h4>@</h4>Responses will only be displayed to the account holder that issued the command.  The command issuer will see ""Phogue has been kicked for team killing"" but no one else will.</blockquote>
	<blockquote><h4>#</h4>Responses will be displayed to all players that hold an account (""display to admins only"").  Only account holders will see ""Phogue has been kicked for team killing"".</blockquote>

	<p>All error messages are privately sent to the command issuer</p>

<h2>Settings</h2>
	<h3>Banning</h3>
		<blockquote><h4>Ban Type</h4>
			<ul>
				<li><b>Frostbite - Name (default)</b>: Bans on the players name alone.  This is the weakest type of ban.</li>
				<li><b>Frostbite - EA GUID</b>: Bans on the players EA GUID which is tied to their login account.</li>
				<li><b>Punkbuster - GUID</b>: Bans a player on their punkbuster GUID.</li>
			</ul>
		</blockquote>
		<blockquote><h4>Include Admin name</h4>
			<ul>
				If enabled the plugin will include the admin name responsible for the ban in the reason.
			</ul>
		</blockquote>

<h2>History</h2>
	<h3>3.3.0.7</h3>
		<ul>
			<li>Minor startup performance optimization</li>
		</ul>
	<h3>3.3.0.4</h3>
		<ul>
			<li>added endround command</li>
		</ul>
	<h3>3.3.0.0</h3>
		<ul>
			<li>Updated to support BF3</li>
		</ul>

<h2>Commands (default)</h2>

	<h3>Kicking / Banning</h3>
		<blockquote><h4>@kick [playername] [optional: reason]</h4>
			<ul>
				<li>Kicks a player from your server</li>
				<li>The reason will appear in the response and is used to display to the player on the games menu</li>
				<li>Requirements: Minimum of kick privileges</li>
			</ul>
		</blockquote>

		<blockquote><h4>@tban [playername] [time in minutes] [optional: reason]</h4>
			<ul>
				<li>Temporarily bans a player from your server for a set time in minutes</li>
				<li>The reason will appear in the response and is used to display to the player on the games menu</li>
				<li>By default procon will not allow users with only temporary ban privileges to ban for longer than an hour</li>
				<li>Requirements: Minimum of temporary ban privileges</li>
			</ul>
		</blockquote>

		<blockquote><h4>@ban [playername] [optional: reason]</h4>
			<ul>
				<li>Permanently bans a player from your server</li>
				<li>The reason will appear in the response and is used to display to the player on the games menu</li>
				<li>Requirements: Permanent ban privileges</li>
			</ul>
		</blockquote>

	<h3>Killing</h3>
		<blockquote><h4>@kill [playername] [optional: reason]</h4>
			<ul>
				<li>Kills a player</li>
				<li>The reason will appear in the response and message sent to the player</li>
				<li>Requirements: Minimum of kill privileges</li>
			</ul>
		</blockquote>

		<blockquote><h4>@nuke [optional: team]</h4>
			<ul>
				<li>Kills every player in the server, or if a team is specified will kill every player on that team</li>
				<li>Has a 10 seconds countdown that can be canceled with the @cancel command</li>
				<li>Requirements: Minimum of kill privileges</li>
			</ul>
		</blockquote>

	<h3>Communication</h3>
		<blockquote><h4>@say [text]</h4>
			<ul>
				<li>Says [text] for everyone to see from the Server</li>
				<li>Requirements: Must hold an account with procon</li>
			</ul>
		</blockquote>

		<blockquote><h4>@psay [playername] [text]</h4>
			<ul>
				<li>Says [text] to a specific player from the Server</li>
				<li>Requirements: Must hold an account with procon</li>
			</ul>
		</blockquote>

		<blockquote><h4>@yell [text]</h4><small>BFBC2 & BF3 only</small>
			<ul>
				<li>Yells [text] for everyone to see in the middle of their screen</li>
				<li>Requirements: Must hold an account with procon</li>
			</ul>
		</blockquote>

		<blockquote><h4>@pyell [playername] [text]</h4><small>BFBC2 & BF3 only</small>
			<ul>
				<li>Yells [text] to a specific player in the middle of their screen</li>
				<li>Requirements: Must hold an account with procon</li>
			</ul>
		</blockquote>

	<h3>Map controls</h3>
		<blockquote><h4>@restart [optional: timer]</h4>
			<ul>
				<li>Restarts the current map</li>
				<li>If specified will display a countdown timer than can be canceled with the @cancel command</li>
				<li>Requirements: Must have ""Use Map Functions"" privileges</li>
			</ul>
		</blockquote>

		<blockquote><h4>@nextlevel [optional: timer]</h4>
			<ul>
				<li>Forwards to the next level</li>
				<li>If specified will display a countdown timer than can be canceled with the @cancel command</li>
				<li>Requirements: Must have ""Use Map Functions"" privileges</li>
			</ul>
		</blockquote>

		<blockquote><h4>@endround [teamID] [optional: timer]</h4>
			<ul>
				<li>Ends the round and declares the team given by its number as winner</li>
				<li>If specified will display a countdown timer than can be canceled with the @cancel command</li>
				<li>Requirements: Must have ""Use Map Functions"" privileges</li>
			</ul>
		</blockquote>

	<h3>Player control</h3>

		<blockquote><h4>@move [playername]</h4>
			<ul>
				<li>Moves a player to another team.  Swaps them in Rush, Conquest, Squad Rush and will cycle through the 4 teams in Squad Deathmatch</li>
				<li>Queues the player and will move them when the next die</li>
				<li>Requirements: Must be able to move players between teams and squads privileges</li>
			</ul>
		</blockquote>

		<blockquote><h4>@move [playername] [squad]</h4><small>BFBC2 & BF3 only</small>
			<ul>
				<li>Moves a player into a squad on their same team</li>
				<li>Queues the player and will move them when the next die</li>
				<li>Requirements: Must be able to move players between teams and squads privileges</li>
			</ul>
		</blockquote>

		<blockquote><h4>@move [playername] [team]</h4>
			<ul>
				<li>Moves a player onto another team in no squad</li>
				<li>Queues the player and will move them when the next die</li>
				<li>Requirements: Must be able to move players between teams and squads privileges</li>
			</ul>
		</blockquote>

		<blockquote><h4>@move [playername] [team] [squad]</h4><small>BFBC2 & BF3 only</small>
			<ul>
				<li>Moves a player onto another team and into a specific squad</li>
				<li>Queues the player and will move them when the next die</li>
				<li>Requirements: Must be able to move players between teams and squads privileges</li>
			</ul>
		</blockquote>

		<blockquote><h4>@fmove</h4>
			<ul>
				<li><b><span style=""color: #ff0000;"">f</span>move</b> functions exactly like all of the @move commands except forces the move immediately by killing the player</li>
				<li>Requirements: Must be able to move players between teams and squads privileges</li>
			</ul>
		</blockquote>

	<h3>Countdowns</h3>
		<blockquote><h4>@cancel</h4>
			<ul>
				<li>Cancels all countdowns initiated by the account holder</li>
				<li>You cannot cancel other account holders countdowns</li>
				<li>Requirements: Must hold an account with procon</li>
			</ul>
		</blockquote>

	<h3>Custom Configs (basic in game command addons)</h3>
		<blockquote><h4>@exec</h4>
			<ul>
				<li>Displays a list of configs available in the /Configs/CInGameAdmin/*.cfg directory</li>
				<li>This command replaces the ""@exec -l"" command of 1.6 and prior versions of the in game admin</li>
				<li>Requirements: Must hold an account with procon (the configs validate privileges themselves)</li>
			</ul>
		</blockquote>

		<blockquote><h4>@exec [configname]</h4>
			<ul>
				<li>Executes a config in the /Configs/CInGameAdmin/*.cfg directory</li>
				<li>The config author is responsible for insuring the executing account holder has sufficient privileges</li>
				<li>No pre-made configs are provided with procon, but you can visit <a href=""http://phogue.net/forum/"" target=""_blank"">the forums</a> to download configs to extend the in game admin</li>
				<li>You can make/edit/copy/whatever any more configs, essentially allowing you to make your own basic in game commands =)</li>
				<li>Requirements: Must hold an account with procon (the configs validate privileges themselves)</li>
			</ul>
		</blockquote>

	<p>If procon needs confirmation of your intentions it will ask ""Did you mean [command]?"", you can then answer !yes which will execute your confirmed command.</p>

<h2>Argument Matching</h2>

	<p>Procon will match your command against a dictionary of possible arguments, then order the possibilities by those containing subsets of what you said.</p>

	<p>Possibilities: ""Phogue"", ""1349"", ""Sunder"", ""Zangen"", ""Sinex""</p>
	<ul>
		<li>""Sangen"" will match ""Zangen"" because only one letter is different and no possibilities have a subset of ""Sangen""</li>
		<li>""sin"" will match ""Sinex"" because even though it is lexicographically closer to ""1349"", Sinex contains a subset of ""sin""</li>
	</ul>

<h2>Additional Information</h2>
	<ul>
		<li>This plugin is compatible with Basic In-Game Info's @help command</li>
		<li>In game admin will use Vanilla bfbc2 data if available, then use punkbuster data if available.</li>
		<li>The in game admin uses a combination of a spell checker with an auto-completer.  As of 1.5 the spell checker will favour names/maps/etc that have a subset of what you typed exactly.  Play around with it and let me know your horror stories with this feature on the forums =)</li>
		<li>No one will see you issuing the commands (even you) if you prefix it with a '/'
			<blockquote>
				/@kick Phogue<br>
				(this text won't be displayed in the game)
			</blockquote>
		</li>
	</ul>
";
        }

        public void OnPluginLoadingEnv(List<string> lstPluginEnv)
        {
            Version PRoConVersion = new Version(lstPluginEnv[0]);
            this.m_strPRoConVersion = PRoConVersion.ToString();
            this.m_strServerGameType = lstPluginEnv[1].ToLower();
            this.m_strGameMod = lstPluginEnv[2];
            this.m_strServerVersion = lstPluginEnv[3];

            if (String.Compare(this.m_strServerGameType, "bf3", true) == 0 || String.Compare(this.m_strServerGameType, "bf4", true) == 0 || String.Compare(this.m_strServerGameType, "bfhl", true) == 0)
            {
                this.m_iYellDivider = 1000;
                this.m_strShowMessageLength = (this.m_iShowMessageLength / this.m_iYellDivider).ToString();
            }
            if (String.Compare(this.m_strServerGameType, "mohw", true) == 0)
            {
                this.m_blHasYellDuration = false;
            }
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnServerInfo", "OnLoadingLevel", "OnLogin", "OnPlayerTeamChange", "OnPlayerSquadChange", "OnPlayerJoin", "OnPlayerLeft", "OnPunkbusterPlayerInfo", "OnListPlayers", "OnPlayerKilled");
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bIn-Game Admin ^2Enabled!");

            this.m_squadNames.Clear();
            this.m_squadNames.Add("None");
            this.m_squadNames.Add("Alpha");
            this.m_squadNames.Add("Bravo");
            this.m_squadNames.Add("Charlie");
            this.m_squadNames.Add("Delta");
            this.m_squadNames.Add("Echo");
            this.m_squadNames.Add("Foxtrot");
            this.m_squadNames.Add("Golf");
            this.m_squadNames.Add("Hotel");
            this.m_squadNames.Add("India");
            this.m_squadNames.Add("Juliet");
            this.m_squadNames.Add("Kilo");
            this.m_squadNames.Add("Lima");
            this.m_squadNames.Add("Mike");
            this.m_squadNames.Add("November");
            this.m_squadNames.Add("Oscar");
            this.m_squadNames.Add("Papa");
            this.m_squadNames.Add("Quebec");
            this.m_squadNames.Add("Romeo");
            this.m_squadNames.Add("Sierra");
            this.m_squadNames.Add("Tango");
            this.m_squadNames.Add("Uniform");
            this.m_squadNames.Add("Victor");
            this.m_squadNames.Add("Whiskey");
            this.m_squadNames.Add("Xray");
            this.m_squadNames.Add("Yankee");
            this.m_squadNames.Add("Zulu");
            this.m_squadNames.Add("Haggard");
            this.m_squadNames.Add("Sweetwater");
            this.m_squadNames.Add("Preston");
            this.m_squadNames.Add("Redford");
            this.m_squadNames.Add("Faith");
            this.m_squadNames.Add("Celeste");

            this.m_isPluginEnabled = true;
            this.RegisterAllCommands();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bIn-Game Admin ^1Disabled =(");

            this.m_isPluginEnabled = false;
            this.UnregisterAllCommands(true);
        }

        // GetDisplayPluginVariables and GetPluginVariables Lists only variables you want shown.. for
        // instance enabling one option might hide another option It's the best I got until I
        // implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Response Scope|Private Prefix", this.m_strPrivatePrefix.GetType(), this.m_strPrivatePrefix));
            lstReturn.Add(new CPluginVariable("Response Scope|Admins Prefix", this.m_strAdminsPrefix.GetType(), this.m_strAdminsPrefix));
            lstReturn.Add(new CPluginVariable("Response Scope|Public Prefix", this.m_strPublicPrefix.GetType(), this.m_strPublicPrefix));

            lstReturn.Add(new CPluginVariable("Commands|Kick", this.m_strKickCommand.GetType(), this.m_strKickCommand));

            lstReturn.Add(new CPluginVariable("Commands|Move", this.m_strMoveCommand.GetType(), this.m_strMoveCommand));
            lstReturn.Add(new CPluginVariable("Commands|Force Move", this.m_strForceMoveCommand.GetType(), this.m_strForceMoveCommand));
            lstReturn.Add(new CPluginVariable("Commands|Nuke", this.m_strNukeCommand.GetType(), this.m_strNukeCommand));
            lstReturn.Add(new CPluginVariable("Commands|Kill", this.m_strKillCommand.GetType(), this.m_strKillCommand));
            lstReturn.Add(new CPluginVariable("Commands|Temporary Ban", this.m_strTemporaryBanCommand.GetType(), this.m_strTemporaryBanCommand));
            lstReturn.Add(new CPluginVariable("Commands|Permanent Ban", this.m_strPermanentBanCommand.GetType(), this.m_strPermanentBanCommand));
            lstReturn.Add(new CPluginVariable("Commands|Say", this.m_strSayCommand.GetType(), this.m_strSayCommand));
            lstReturn.Add(new CPluginVariable("Commands|Player Say", this.m_strPlayerSayCommand.GetType(), this.m_strPlayerSayCommand));
            lstReturn.Add(new CPluginVariable("Commands|Yell", this.m_strYellCommand.GetType(), this.m_strYellCommand));
            lstReturn.Add(new CPluginVariable("Commands|Player Yell", this.m_strPlayerYellCommand.GetType(), this.m_strPlayerYellCommand));
            //lstReturn.Add(new CPluginVariable("Commands|Player Warn", this.m_strPlayerWarnCommand.GetType(), this.m_strPlayerWarnCommand));
            lstReturn.Add(new CPluginVariable("Commands|Restart Map", this.m_strRestartLevelCommand.GetType(), this.m_strRestartLevelCommand));
            lstReturn.Add(new CPluginVariable("Commands|Next Map", this.m_strNextLevelCommand.GetType(), this.m_strNextLevelCommand));
            lstReturn.Add(new CPluginVariable("Commands|End Round", this.m_strEndLevelCommand.GetType(), this.m_strEndLevelCommand));
            lstReturn.Add(new CPluginVariable("Commands|Confirm Selection", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));
            lstReturn.Add(new CPluginVariable("Commands|Cancel command", this.m_strCancelCommand.GetType(), this.m_strCancelCommand));
            lstReturn.Add(new CPluginVariable("Commands|Execute config command", this.m_strExecuteConfigCommand.GetType(), this.m_strExecuteConfigCommand));

            lstReturn.Add(new CPluginVariable("Banning|Ban Type", "enum.CInGameAdmin_BanType(Frostbite - Name|Frostbite - EA GUID|Punkbuster - GUID)", this.m_strBanTypeOption));
            lstReturn.Add(new CPluginVariable("Banning|Include Admin name", typeof(enumBoolYesNo), this.m_enBanIncAdmin));

            lstReturn.Add(new CPluginVariable("Responses|Show responses (seconds)", this.m_iShowMessageLength.GetType(), this.m_iShowMessageLength / 1000));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Private Prefix", this.m_strPrivatePrefix.GetType(), this.m_strPrivatePrefix));
            lstReturn.Add(new CPluginVariable("Admins Prefix", this.m_strAdminsPrefix.GetType(), this.m_strAdminsPrefix));
            lstReturn.Add(new CPluginVariable("Public Prefix", this.m_strPublicPrefix.GetType(), this.m_strPublicPrefix));

            lstReturn.Add(new CPluginVariable("Kick", this.m_strKickCommand.GetType(), this.m_strKickCommand));
            lstReturn.Add(new CPluginVariable("Nuke", this.m_strNukeCommand.GetType(), this.m_strNukeCommand));
            lstReturn.Add(new CPluginVariable("Kill", this.m_strKillCommand.GetType(), this.m_strKillCommand));
            lstReturn.Add(new CPluginVariable("Move", this.m_strMoveCommand.GetType(), this.m_strMoveCommand));
            lstReturn.Add(new CPluginVariable("Force Move", this.m_strForceMoveCommand.GetType(), this.m_strForceMoveCommand));
            lstReturn.Add(new CPluginVariable("Temporary Ban", this.m_strTemporaryBanCommand.GetType(), this.m_strTemporaryBanCommand));
            lstReturn.Add(new CPluginVariable("Permanent Ban", this.m_strPermanentBanCommand.GetType(), this.m_strPermanentBanCommand));
            lstReturn.Add(new CPluginVariable("Say", this.m_strSayCommand.GetType(), this.m_strSayCommand));
            lstReturn.Add(new CPluginVariable("Player Say", this.m_strPlayerSayCommand.GetType(), this.m_strPlayerSayCommand));
            lstReturn.Add(new CPluginVariable("Yell", this.m_strYellCommand.GetType(), this.m_strYellCommand));
            lstReturn.Add(new CPluginVariable("Player Yell", this.m_strPlayerYellCommand.GetType(), this.m_strPlayerYellCommand));
            //lstReturn.Add(new CPluginVariable("Player Warn", this.m_strPlayerWarnCommand.GetType(), this.m_strPlayerWarnCommand));
            lstReturn.Add(new CPluginVariable("Restart Map", this.m_strRestartLevelCommand.GetType(), this.m_strRestartLevelCommand));
            lstReturn.Add(new CPluginVariable("Next Map", this.m_strNextLevelCommand.GetType(), this.m_strNextLevelCommand));
            lstReturn.Add(new CPluginVariable("End Round", this.m_strEndLevelCommand.GetType(), this.m_strEndLevelCommand));
            lstReturn.Add(new CPluginVariable("Confirm Selection", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));
            lstReturn.Add(new CPluginVariable("Cancel command", this.m_strCancelCommand.GetType(), this.m_strCancelCommand));
            lstReturn.Add(new CPluginVariable("Execute config command", this.m_strExecuteConfigCommand.GetType(), this.m_strExecuteConfigCommand));

            lstReturn.Add(new CPluginVariable("Ban Type", "enum.CInGameAdmin_BanType(Frostbite - Name|Frostbite - EA GUID|Punkbuster - GUID)", this.m_strBanTypeOption));
            lstReturn.Add(new CPluginVariable("Include Admin name", typeof(enumBoolYesNo), this.m_enBanIncAdmin));

            lstReturn.Add(new CPluginVariable("Show responses (seconds)", this.m_iShowMessageLength.GetType(), this.m_iShowMessageLength / 1000));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iTimeSeconds = 8;
            this.UnregisterAllCommands(false);

            if (strVariable.CompareTo("Private Prefix") == 0)
            {
                this.m_strPrivatePrefix = strValue;
            }
            else if (strVariable.CompareTo("Admins Prefix") == 0)
            {
                this.m_strAdminsPrefix = strValue;
            }
            else if (strVariable.CompareTo("Public Prefix") == 0)
            {
                this.m_strPublicPrefix = strValue;
            }
            else if (strVariable.CompareTo("Kick") == 0)
            {
                this.m_strKickCommand = strValue;
            }
            else if (strVariable.CompareTo("Kill") == 0)
            {
                this.m_strKillCommand = strValue;
            }
            else if (strVariable.CompareTo("Nuke") == 0)
            {
                this.m_strNukeCommand = strValue;
            }
            else if (strVariable.CompareTo("Move") == 0)
            {
                this.m_strMoveCommand = strValue;
            }
            else if (strVariable.CompareTo("Force Move") == 0)
            {
                this.m_strForceMoveCommand = strValue;
            }
            else if (strVariable.CompareTo("Temporary Ban") == 0)
            {
                this.m_strTemporaryBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Permanent Ban") == 0)
            {
                this.m_strPermanentBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Say") == 0)
            {
                this.m_strSayCommand = strValue;
            }
            else if (strVariable.CompareTo("Player Say") == 0)
            {
                this.m_strPlayerSayCommand = strValue;
            }
            else if (strVariable.CompareTo("Yell") == 0)
            {
                this.m_strYellCommand = strValue;
            }
            else if (strVariable.CompareTo("Player Yell") == 0)
            {
                this.m_strPlayerYellCommand = strValue;
            }
            //else if (strVariable.CompareTo("Player Warn") == 0) {
            //    this.m_strPlayerWarnCommand = strValue;
            //}
            else if (strVariable.CompareTo("Restart Map") == 0)
            {
                this.m_strRestartLevelCommand = strValue;
            }
            else if (strVariable.CompareTo("Next Map") == 0)
            {
                this.m_strNextLevelCommand = strValue;
            }
            else if (strVariable.CompareTo("End Round") == 0)
            {
                this.m_strEndLevelCommand = strValue;
            }
            else if (strVariable.CompareTo("Confirm Selection") == 0)
            {
                this.m_strConfirmCommand = strValue;
            }
            else if (strVariable.CompareTo("Cancel command") == 0)
            {
                this.m_strCancelCommand = strValue;
            }
            else if (strVariable.CompareTo("Execute config command") == 0)
            {
                this.m_strExecuteConfigCommand = strValue;
            }
            else if (strVariable.CompareTo("Ban Type") == 0)
            {
                this.m_strBanTypeOption = strValue;

                if (String.Compare("Frostbite - Name", this.m_strBanTypeOption, true) == 0)
                {
                    this.m_dicPlayers.BanMethod = BanTypes.FrostbiteName;
                }
                else if (String.Compare("Frostbite - EA GUID", this.m_strBanTypeOption, true) == 0)
                {
                    this.m_dicPlayers.BanMethod = BanTypes.FrostbiteEaGuid;
                }
                else if (String.Compare("Punkbuster - GUID", this.m_strBanTypeOption, true) == 0)
                {
                    this.m_dicPlayers.BanMethod = BanTypes.PunkbusterGuid;
                }
            }
            else if (strVariable.CompareTo("Include Admin name") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enBanIncAdmin = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                this.m_dicPlayers.m_enBanIncAdmin = this.m_enBanIncAdmin;
            }
            else if (strVariable.CompareTo("Show responses (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iShowMessageLength = iTimeSeconds * 1000;

                if (iTimeSeconds <= 0)
                {
                    this.m_iShowMessageLength = 1000;
                }
                else if (iTimeSeconds >= 60)
                {
                    this.m_iShowMessageLength = 59000;
                }

                this.m_strShowMessageLength = (this.m_iShowMessageLength / this.m_iYellDivider).ToString();
            }

            this.RegisterAllCommands();
        }

        private void UnregisterAllCommands(bool force)
        {
            List<string> emptyList = new List<string>();

            if (this.m_isPluginEnabled == true || force == true)
            {
                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strKickCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "optional: reason",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strKillCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "optional: reason",
                                emptyList
                            )
                        )
                    )
                );

                #region Move Command Registration

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strMoveCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strMoveCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "team",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strMoveCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "squad",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strMoveCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "team",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "squad",
                                emptyList
                            )
                        )
                    )
                );

                #endregion

                #region Force Move Command Registration

                // Force Move

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strForceMoveCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strForceMoveCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "team",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strForceMoveCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "squad",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strForceMoveCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "team",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "squad",
                                emptyList
                            )
                        )
                    )
                );

                #endregion

                #region Nuking

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strNukeCommand,
                        this.Listify<MatchArgumentFormat>()
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strNukeCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "team",
                                emptyList
                            )
                        )
                    )
                );

                #endregion

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strCancelCommand,
                        this.Listify<MatchArgumentFormat>()
                    )
                );

                #region Perm/Temp Banning

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strTemporaryBanCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "time in minutes",
                                MatchArgumentFormatTypes.Regex,
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "optional: reason",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strPermanentBanCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "optional: reason",
                                emptyList
                            )
                        )
                    )
                );

                #endregion

                #region Say/Yell Commands

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strSayCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "text",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strPlayerSayCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "text",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strYellCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "text",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strPlayerYellCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "text",
                                emptyList
                            )
                        )
                    )
                );

                #endregion

                #region Restart/Next map functions

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strRestartLevelCommand,
                        this.Listify<MatchArgumentFormat>()
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strRestartLevelCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "countdown between 0 and 60 seconds",
                                MatchArgumentFormatTypes.Regex,
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strNextLevelCommand,
                        this.Listify<MatchArgumentFormat>()
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strNextLevelCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "optional: countdown between 0 and 60 seconds",
                                MatchArgumentFormatTypes.Regex,
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strEndLevelCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "teamID",
                                emptyList
                            )
                        )
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strEndLevelCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "teamID",
                                emptyList
                            ),
                            new MatchArgumentFormat(
                                "optional: countdown between 0 and 60 seconds",
                                MatchArgumentFormatTypes.Regex,
                                emptyList
                            )
                        )
                    )
                );

                #endregion

                #region Config Execution

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strExecuteConfigCommand,
                        this.Listify<MatchArgumentFormat>()
                    )
                );

                this.UnregisterCommand(
                    new MatchCommand(
                        emptyList,
                        this.m_strExecuteConfigCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "config",
                                emptyList
                            )
                        )
                    )
                );

                #endregion
            }
        }

        private void RegisterAllCommands()
        {
            if (this.m_isPluginEnabled == true)
            {
                List<string> scopes = this.Listify<string>(this.m_strPrivatePrefix, this.m_strAdminsPrefix, this.m_strPublicPrefix);
                List<string> emptyList = new List<string>();

                MatchCommand confirmationCommand = new MatchCommand(scopes, this.m_strConfirmCommand, this.Listify<MatchArgumentFormat>());

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandKick",
                        scopes,
                        this.m_strKickCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                this.m_dicPlayers.GetSoldierNameKeys()
                            ),
                            new MatchArgumentFormat(
                                "optional: reason",
                                emptyList)
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanKickPlayers,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges to kick players"),
                        "Kicks a player with an optional reason"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandKill",
                        scopes,
                        this.m_strKillCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                this.m_dicPlayers.GetSoldierNameKeys()
                            ),
                            new MatchArgumentFormat(
                                "optional: reason",
                                emptyList)
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanKillPlayers,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges to kill players"),
                        "Kills a player with an optional reason"
                    )
                );

                #region Nuking Everyone

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandNukeEveryone",
                        scopes,
                        this.m_strNukeCommand,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanKillPlayers,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges to nuke everyone"),
                        "Kills everyone proceeded by a 10 second nuke countdown"
                    )
                );

                #endregion

                if (this.m_currentMap != null)
                {
                    List<string> teamNamesForMap = new List<string>(); // or add playlist to game.def's
                    if (this.m_strServerGameType == "bfbc2")
                    {
                        teamNamesForMap = this.GetTeamList("{TeamName}", this.m_currentMap.PlayList);
                    }
                    else
                    {
                        teamNamesForMap = this.GetTeamListByPlayList("{TeamName}", this.m_currentMap.PlayList);
                    }
                    // this.ExecuteCommand("procon.protected.pluginconsole.write", "1 -- " +
                    // this.m_currentMap.PlayList + " -- " + teamNamesForMap.Count.ToString()
                    // + " --- | " + String.Join(", ", teamNamesForMap.ToArray()));

                    #region Nuking Teams

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandNukeTeam",
                            scopes,
                            this.m_strNukeCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "team",
                                    teamNamesForMap
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Privileges,
                                Privileges.CanKillPlayers,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to nuke a team"),
                            "Kills everyone on a team proceeded by a 10 second nuke countdown"
                        )
                    );

                    #endregion

                    // Since SQDM team names are the same as squad names I canno split these up into
                    // seperate callbacks

                    #region Move Command Registration

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandMove",
                            scopes,
                            this.m_strMoveCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "playername",
                                    this.m_dicPlayers.GetSoldierNameKeys()
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Privileges,
                                Privileges.CanMovePlayers,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to move players"),
                            "Cycles a player through the available teams next death"
                        )
                    );

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandMove",
                            scopes,
                            this.m_strMoveCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "playername",
                                    this.m_dicPlayers.GetSoldierNameKeys()
                                ),
                                new MatchArgumentFormat(
                                    "team",
                                    teamNamesForMap
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Privileges,
                                Privileges.CanMovePlayers,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to move players"),
                            "Moves a player to a specific team next death"
                        )
                    );

                    // Force Move

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandForceMove",
                            scopes,
                            this.m_strForceMoveCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "playername",
                                    this.m_dicPlayers.GetSoldierNameKeys()
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Privileges,
                                Privileges.CanMovePlayers,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to move players"),
                            "Kills a player and cycles them through the available teams"
                        )
                    );

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandForceMove",
                            scopes,
                            this.m_strForceMoveCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "playername",
                                    this.m_dicPlayers.GetSoldierNameKeys()
                                ),
                                new MatchArgumentFormat(
                                    "team",
                                    teamNamesForMap
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Privileges,
                                Privileges.CanMovePlayers,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to move players"),
                            "Kills a player and moves a player to a specific team"
                        )
                    );

                    #endregion

                    // Include game specific commands BFBC2 specific commands

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandYell",
                            scopes,
                            this.m_strYellCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "text",
                                    emptyList
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Account,
                                "You do not have enough privileges to server yell"),
                            "Yells a message via the server to all players"
                        )
                    );

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandPlayerYell",
                            scopes,
                            this.m_strPlayerYellCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "playername",
                                    this.m_dicPlayers.GetSoldierNameKeys()
                                ),
                                new MatchArgumentFormat(
                                    "text",
                                    emptyList
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Account,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to server yell to a player"),
                            "Yells a message via the server to a single player"
                        )
                    );

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandMove",
                            scopes,
                            this.m_strMoveCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "playername",
                                    this.m_dicPlayers.GetSoldierNameKeys()
                                ),
                                new MatchArgumentFormat(
                                    "squad",
                                    this.m_squadNames
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Privileges,
                                Privileges.CanMovePlayers,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to move players"),
                            "Moves a player to a specific squad on the same team next death"
                        )
                    );

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandMove",
                            scopes,
                            this.m_strMoveCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "playername",
                                    this.m_dicPlayers.GetSoldierNameKeys()
                                ),
                                new MatchArgumentFormat(
                                    "team",
                                    teamNamesForMap
                                ),
                                new MatchArgumentFormat(
                                    "squad",
                                    this.m_squadNames
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Privileges,
                                Privileges.CanMovePlayers,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to move players"),
                            "Moves a player to a specific team and squad next death"
                        )
                    );

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandForceMove",
                            scopes,
                            this.m_strForceMoveCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "playername",
                                    this.m_dicPlayers.GetSoldierNameKeys()
                                ),
                                new MatchArgumentFormat(
                                    "squad",
                                    this.m_squadNames
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Privileges,
                                Privileges.CanMovePlayers,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to move players"),
                            "Kills a player and moves them to a specific squad on the same team"
                        )
                    );

                    this.RegisterCommand(
                        new MatchCommand(
                            "CInGameAdmin",
                            "OnCommandForceMove",
                            scopes,
                            this.m_strForceMoveCommand,
                            this.Listify<MatchArgumentFormat>(
                                new MatchArgumentFormat(
                                    "playername",
                                    this.m_dicPlayers.GetSoldierNameKeys()
                                ),
                                new MatchArgumentFormat(
                                    "team",
                                    teamNamesForMap
                                ),
                                new MatchArgumentFormat(
                                    "squad",
                                    this.m_squadNames
                                )
                            ),
                            new ExecutionRequirements(
                                ExecutionScope.Privileges,
                                Privileges.CanMovePlayers,
                                2,
                                confirmationCommand,
                                "You do not have enough privileges to move players"),
                            "Kills a player and moves them to a specific team and squad"
                        )
                    );
                }

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandCancel",
                        scopes,
                        this.m_strCancelCommand,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(
                            ExecutionScope.Account,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges to cancel commands"),
                        "Cancels a countdown timer"
                    )
                );

                #region Perm/Temp Banning

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandTemporaryBan",
                        scopes,
                        this.m_strTemporaryBanCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                this.m_dicPlayers.GetSoldierNameKeys()
                            ),
                            new MatchArgumentFormat(
                                "time in minutes",
                                MatchArgumentFormatTypes.Regex,
                                this.Listify<string>(
                                    "[0-9]*"
                                )
                            ),
                            new MatchArgumentFormat(
                                "optional: reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanTemporaryBanPlayers,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges to temporary ban players"),
                        "Temporarily bans a player for a length of time in minutes"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandPermanentBan",
                        scopes,
                        this.m_strPermanentBanCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                this.m_dicPlayers.GetSoldierNameKeys()
                            ),
                            new MatchArgumentFormat(
                                "optional: reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanPermanentlyBanPlayers,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges to permanently ban players"),
                        "Permanently bans a player with an optional reason"
                    )
                );

                #endregion

                #region Say/Yell Commands

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandSay",
                        scopes,
                        this.m_strSayCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "text",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Account,
                            "You do not have enough privileges to server say"),
                        "Says a message via the server to all players"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandPlayerSay",
                        scopes,
                        this.m_strPlayerSayCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                this.m_dicPlayers.GetSoldierNameKeys()
                            ),
                            new MatchArgumentFormat(
                                "text",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Account,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges to server say to a player"),
                        "Says a message via the server to a single player"
                    )
                );

                #endregion

                #region Restart/Next map functions

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandRestartLevel",
                        scopes,
                        this.m_strRestartLevelCommand,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanUseMapFunctions,
                            0,
                            confirmationCommand,
                            "You do not have enough privileges to restart the map"),
                        "Immediately restarts the map"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandRestartLevelCountdown",
                        scopes,
                        this.m_strRestartLevelCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "countdown between 0 and 60 seconds",
                                MatchArgumentFormatTypes.Regex,
                                this.Listify<string>(
                                    "[0-9]+"
                                )
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanUseMapFunctions,
                            0,
                            confirmationCommand,
                            "You do not have enough privileges to restart the map"),
                        "Restarts the current map with an optional countdown"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandNextLevel",
                        scopes,
                        this.m_strNextLevelCommand,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanUseMapFunctions,
                            0,
                            confirmationCommand,
                            "You do not have enough privileges to forward the map"),
                        "Immediately forwards the map"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandNextLevelCountdown",
                        scopes,
                        this.m_strNextLevelCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "optional: countdown between 0 and 60 seconds",
                                MatchArgumentFormatTypes.Regex,
                                this.Listify<string>(
                                    "[0-9]+"
                                )
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanUseMapFunctions,
                            0,
                            confirmationCommand,
                            "You do not have enough privileges to forward the map"),
                        "Forwards the map with an optional countdown"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandEndLevel",
                        scopes,
                        this.m_strEndLevelCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "teamID",
                                this.Listify<string>("1", "2", "3", "4")
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanUseMapFunctions,
                            0,
                            confirmationCommand,
                            "You do not have enough privileges to end the round"),
                        "Immediately forwards the map"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandEndLevelCountdown",
                        scopes,
                        this.m_strNextLevelCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "teamID",
                                this.Listify<string>("1", "2", "3", "4")
                            ),
                            new MatchArgumentFormat(
                                "optional: countdown between 0 and 60 seconds",
                                MatchArgumentFormatTypes.Regex,
                                this.Listify<string>(
                                    "[0-9]+"
                                )
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanUseMapFunctions,
                            0,
                            confirmationCommand,
                            "You do not have enough privileges to end the round"),
                        "Forwards the map with an optional countdown"
                    )
                );

                #endregion

                #region Config Execution

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandListConfigs",
                        scopes,
                        this.m_strExecuteConfigCommand,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(
                            ExecutionScope.Account,
                            "You do not have enough privileges to list available configs"),
                        "Lists configs available in the ./Configs/CInGameAdmin/ directory"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "CInGameAdmin",
                        "OnCommandExecuteConfig",
                        scopes,
                        this.m_strExecuteConfigCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "config",
                                this.GetConfigList()
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Account,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges to execute a config"),
                        "Executes a config located in ./Configs/CInGameAdmin/ directory"
                    )
                );

                #endregion
            }
        }

        private List<string> GetConfigList()
        {
            List<string> returnList = new List<string>();

            string inGameAdminConfigsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(Path.Combine(Path.Combine("..", ".."), "Configs"), "CInGameAdmin"));

            if (Directory.Exists(inGameAdminConfigsDirectory) == true)
            {
                DirectoryInfo diConfigsDir = new DirectoryInfo(inGameAdminConfigsDirectory);
                FileInfo[] a_fiConfigs = diConfigsDir.GetFiles("*.cfg");

                for (int i = 0; i < a_fiConfigs.Length; i++)
                {
                    returnList.Add(Regex.Replace(a_fiConfigs[i].Name, "\\.cfg$", ""));
                }
            }

            return returnList;
        }

        private void m_dicPlayers_ExecuteCommand(params string[] words)
        {
            this.ExecuteCommand(words);
        }

        private void m_dicPlayers_SendResponse(string strScope, string strAccountName, string strMessage)
        {
            if (String.Compare(strScope, this.m_strPrivatePrefix) == 0)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", strMessage, "player", strAccountName);
            }
            else if (String.Compare(strScope, this.m_strPublicPrefix) == 0)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", strMessage, "all");
            }
            else if (String.Compare(strScope, this.m_strAdminsPrefix) == 0)
            {
                CPrivileges cpAccount = null;

                foreach (string soldierNames in this.m_dicPlayers.GetSoldierNameKeys())
                {
                    cpAccount = this.GetAccountPrivileges(soldierNames);

                    if (cpAccount != null && cpAccount.PrivilegesFlags > 0)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", strMessage, "player", soldierNames);
                    }
                }
            }
        }

        public void OnCommandKick(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Attempting to kick {0}{1}", capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments.Length == 0 ? String.Empty : " for " + capCommand.ExtraArguments), strSpeaker);

            this.m_dicPlayers.KickPlayer(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments);
        }

        public void OnCommandKill(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Attempting to kill {0}{1}", capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments.Length == 0 ? String.Empty : " for " + capCommand.ExtraArguments), strSpeaker);

            this.m_dicPlayers.KillPlayer(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments);
        }

        public void OnCommandNukeEveryone(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Initiated a nuke against everybody"), strSpeaker);

            this.m_dicPlayers.Nuke(capCommand.ResposeScope, strSpeaker, new CPlayerSubset(CPlayerSubset.PlayerSubsetType.All), "everyone");
        }

        public void OnCommandNukeTeam(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            int iNukeTargetTeamID = 0;

            if (capCommand.MatchedArguments[0].Argument.Length >= 1 && this.TryGetTeamID(capCommand.MatchedArguments[0].Argument, out iNukeTargetTeamID) == true)
            {
                this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Initiated a nuke against {0}", capCommand.MatchedArguments[0].Argument), strSpeaker);

                this.m_dicPlayers.Nuke(capCommand.ResposeScope, strSpeaker, new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Team, iNukeTargetTeamID), capCommand.MatchedArguments[0].Argument);
            }
        }

        public void OnCommandCancel(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.m_dicPlayers.CancelCountdowns(capCommand.ResposeScope, strSpeaker);
        }

        public void OnCommandTemporaryBan(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            int iTime = 0;
            int iTempBanCeiling = this.GetVariable<int>("TEMP_BAN_CEILING", 3600) / 60;
            CPrivileges cpSpeakerPrivs = this.GetAccountPrivileges(strSpeaker);

            if (int.TryParse(capCommand.MatchedArguments[1].Argument, out iTime) == true)
            {
                if (cpSpeakerPrivs.CanPermanentlyBanPlayers == true)
                {
                    // 0 equals perma ban.
                    if (iTime == 0)
                    {
                        this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Attempting to permanently ban {0}{1}", capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments.Length == 0 ? String.Empty : " for " + capCommand.ExtraArguments), strSpeaker);

                        this.m_dicPlayers.PermanentBanPlayer(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments);
                    }
                    else
                    { // issue temp ban
                        this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Attempting to temporarily ban {0} for {1} seconds{0}", capCommand.MatchedArguments[0].Argument, iTime, capCommand.ExtraArguments.Length == 0 ? String.Empty : " for " + capCommand.ExtraArguments), strSpeaker);

                        this.m_dicPlayers.TemporarilyBanPlayer(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments, iTime);
                    }
                }
                else if (cpSpeakerPrivs.CanTemporaryBanPlayers == true)
                {
                    if (iTime > 0 && iTime <= iTempBanCeiling)
                    {
                        // issue temp ban
                        this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Attempting to temporarily ban {0} for {1} seconds{0}", capCommand.MatchedArguments[0].Argument, iTime, capCommand.ExtraArguments.Length == 0 ? String.Empty : " for " + capCommand.ExtraArguments), strSpeaker);

                        this.m_dicPlayers.TemporarilyBanPlayer(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments, iTime);
                    }
                    else if (iTime <= 0)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "You're not allowed to permanently ban players!", "player", strSpeaker);
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("You're not allowed to ban players longer than {0} minutes!", iTempBanCeiling), "player", strSpeaker);
                    }
                }
            }
        }

        public void OnCommandPermanentBan(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.m_dicPlayers.PermanentBanPlayer(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments);
        }

        public void OnCommandSay(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.ExecuteCommand("procon.protected.send", "admin.say", capCommand.ExtraArguments, "all");
        }

        public void OnCommandPlayerSay(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.m_dicPlayers.PlayerSay(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments);
        }

        public void OnCommandYell(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.m_blHasYellDuration == true)
            {
                this.ExecuteCommand("procon.protected.send", "admin.yell", capCommand.ExtraArguments, this.m_strShowMessageLength, "all");
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.yell", capCommand.ExtraArguments, "all");
            }
        }

        public void OnCommandPlayerYell(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.m_dicPlayers.PlayerYell(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, capCommand.ExtraArguments, this.m_strShowMessageLength, this.m_blHasYellDuration);
        }

        public void OnCommandRestartLevel(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", "Attempting to restart level", strSpeaker);

            this.m_dicPlayers.RestartLevel(capCommand.ResposeScope, strSpeaker);
        }

        public void OnCommandRestartLevelCountdown(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            int iTime = 0;

            if (int.TryParse(capCommand.MatchedArguments[0].Argument, out iTime) == true)
            {
                this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", "Attempting to restart level", strSpeaker);

                this.m_dicPlayers.RestartLevel(capCommand.ResposeScope, strSpeaker, iTime);
            }
        }

        public void OnCommandNextLevel(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", "Attempting to run next level", strSpeaker);

            this.m_dicPlayers.ForwardLevel(capCommand.ResposeScope, strSpeaker);
        }

        public void OnCommandNextLevelCountdown(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            int iTime = 0;

            if (int.TryParse(capCommand.MatchedArguments[0].Argument, out iTime) == true)
            {
                this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", "Attempting to run next level", strSpeaker);

                this.m_dicPlayers.ForwardLevel(capCommand.ResposeScope, strSpeaker, iTime);
            }
        }

        public void OnCommandEndLevel(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            string teamID = capCommand.MatchedArguments[0].Argument;

            if (capCommand.ExtraArguments != String.Empty)
            {
                List<string> lstArguments = this.Wordify(capCommand.ExtraArguments);
                this.OnCommandEndLevelCountdown(strSpeaker, strText, mtcCommand, capCommand, subMatchedScope);
                return;
            }

            this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", "Attempting to end the round", strSpeaker);

            this.m_dicPlayers.EndLevel(capCommand.ResposeScope, strSpeaker, teamID);
        }

        public void OnCommandEndLevelCountdown(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            int iTime = 0;
            string teamID = capCommand.MatchedArguments[0].Argument;

            if (int.TryParse(capCommand.ExtraArguments, out iTime) == true)
            {
                this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Attempting to end the round on countdown ({0})", iTime.ToString()), strSpeaker);

                this.m_dicPlayers.EndLevel(capCommand.ResposeScope, strSpeaker, teamID, iTime);
            }
        }

        public void OnCommandListConfigs(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            string configList = String.Format("Available Configs: {0}", String.Join(", ", this.GetConfigList().ToArray()));

            List<string> lines = this.WordWrap(configList, 100);

            foreach (string line in lines)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", line, "player", strSpeaker);
            }
        }

        public void OnCommandExecuteConfig(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            List<string> lstExecuteCommand = new List<string>();
            List<string> lstArguments = this.Wordify(capCommand.ExtraArguments);
            CPrivileges cpSpeakerPrivs = this.GetAccountPrivileges(strSpeaker);

            string configPath = capCommand.MatchedArguments[0].Argument;

            if (Regex.Match(configPath, ".*\\.cfg").Success == false)
            {
                configPath += ".cfg";
            }

            string inGameAdminConfigsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(Path.Combine(Path.Combine("..", ".."), "Configs"), "CInGameAdmin"));
            configPath = Path.Combine(inGameAdminConfigsDirectory, configPath);

            if (File.Exists(configPath) == true)
            {
                lstExecuteCommand.Add("procon.protected.config.exec");
                lstExecuteCommand.Add(configPath);
                lstExecuteCommand.Add(cpSpeakerPrivs.PrivilegesFlags.ToString());
                lstExecuteCommand.Add(strSpeaker);
                //lstArguments.RemoveAt(0);

                if (lstArguments.Count > 0)
                {
                    lstExecuteCommand.AddRange(lstArguments);
                }

                this.ExecuteCommand(lstExecuteCommand.ToArray());
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Config {0} does not exist!", configPath), "player", strSpeaker);
            }
        }

        private CPlayerSubset GetMoveDestination(string strSpeaker, MatchCommand mtcCommand, CapturedCommand capCommand, out string responseMessage)
        {
            CPlayerInfo cpiMovingPlayer = this.m_dicPlayers[capCommand.MatchedArguments[0].Argument].VanillaInfo;

            responseMessage = String.Empty;

            int iTeamsForMap = this.GetTeamsForMap();
            int iDestinationTeamID = 0;

            string forcedResponse = String.Empty;
            string queuedResponse = String.Empty;

            CPlayerSubset moveDestination = null;

            // Cycle move command
            if (capCommand.MatchedArguments.Count == 1)
            {
                // Rotate their team

                if ((iDestinationTeamID = (cpiMovingPlayer.TeamID + 1) % iTeamsForMap) == 0)
                {
                    iDestinationTeamID = 1;
                }

                queuedResponse = String.Format("{0} will switch teams next death", cpiMovingPlayer.SoldierName);
                forcedResponse = String.Format("Forcing {0} team change", cpiMovingPlayer.SoldierName);

                moveDestination = new CPlayerSubset(CPlayerSubset.PlayerSubsetType.None, iDestinationTeamID, this.GetDefaultSquadIDForMap());
            }
            // Move to team or squad
            else if (capCommand.MatchedArguments.Count == 2)
            {
                // TO DO: Better checking later if they add another gamemode besides squad dm with 5 teams..
                if (iTeamsForMap == 5)
                {
                    // Treat it as a team..
                    if (this.TryGetTeamID(capCommand.MatchedArguments[1].Argument, out iDestinationTeamID) == true)
                    {
                        queuedResponse = String.Format("{0} will be placed in squad {1} next death", cpiMovingPlayer.SoldierName, capCommand.MatchedArguments[1].Argument);
                        forcedResponse = String.Format("Forcing {0} squad change to {1}", cpiMovingPlayer.SoldierName, capCommand.MatchedArguments[1].Argument);

                        moveDestination = new CPlayerSubset(CPlayerSubset.PlayerSubsetType.None, iDestinationTeamID, this.GetDefaultSquadIDForMap());
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Team \"{0}\" is invalid for this map.", capCommand.MatchedArguments[1].Argument), "player", strSpeaker);
                    }
                }
                else
                { // else iTeamsForMap == 3, at least at the moment..
                  // Check if it's a squad name..
                    if (this.m_squadNames.Contains(capCommand.MatchedArguments[1].Argument) == true)
                    {
                        queuedResponse = String.Format("{0} will be placed in squad {1} next death", cpiMovingPlayer.SoldierName, capCommand.MatchedArguments[1].Argument);
                        forcedResponse = String.Format("Forcing {0} squad change to {1}", cpiMovingPlayer.SoldierName, capCommand.MatchedArguments[1].Argument);

                        moveDestination = new CPlayerSubset(CPlayerSubset.PlayerSubsetType.None, cpiMovingPlayer.TeamID, this.GetSquadID(capCommand.MatchedArguments[1].Argument));
                    }
                    else if (this.TryGetTeamID(capCommand.MatchedArguments[1].Argument, out iDestinationTeamID) == true)
                    {
                        queuedResponse = String.Format("{0} will be switched to team {1} next death", cpiMovingPlayer.SoldierName, capCommand.MatchedArguments[1].Argument);
                        forcedResponse = String.Format("Forcing {0} team change to {1}", cpiMovingPlayer.SoldierName, capCommand.MatchedArguments[1].Argument);

                        moveDestination = new CPlayerSubset(CPlayerSubset.PlayerSubsetType.None, iDestinationTeamID, this.GetDefaultSquadIDForMap());
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Team \"{0}\" is invalid for this map.", capCommand.MatchedArguments[1].Argument), "player", strSpeaker);
                    }
                }
            }
            // Move to team + squad
            else if (capCommand.MatchedArguments.Count == 3)
            {
                if (this.TryGetTeamID(capCommand.MatchedArguments[1].Argument, out iDestinationTeamID) == true)
                {
                    if (this.m_squadNames.Contains(capCommand.MatchedArguments[2].Argument) == true)
                    {
                        // It is, move them to the same team but different squad..
                        moveDestination = new CPlayerSubset(CPlayerSubset.PlayerSubsetType.None, iDestinationTeamID, this.GetSquadID(capCommand.MatchedArguments[2].Argument));

                        queuedResponse = String.Format("{0} will be switched to team {1} squad {2} next death", cpiMovingPlayer.SoldierName, capCommand.MatchedArguments[1].Argument, capCommand.MatchedArguments[2].Argument);
                        forcedResponse = String.Format("Forcing {0} team change to {1} squad {2}", cpiMovingPlayer.SoldierName, capCommand.MatchedArguments[1].Argument, capCommand.MatchedArguments[2].Argument);
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Squad \"{0}\" is invalid.", capCommand.MatchedArguments[2].Argument), "player", strSpeaker);
                    }
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Team \"{0}\" is invalid for this map.", capCommand.MatchedArguments[1].Argument), "player", strSpeaker);
                }
            }

            if (moveDestination != null)
            {
                if (String.Compare(capCommand.Command, this.m_strForceMoveCommand, true) == 0)
                {
                    responseMessage = forcedResponse;
                }
                else
                {
                    responseMessage = queuedResponse;
                }
            }

            return moveDestination;
        }

        public void OnCommandMove(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            CPlayerSubset moveDestination = null;
            string response = "";

            try
            {
                if ((moveDestination = this.GetMoveDestination(strSpeaker, mtcCommand, capCommand, out response)) != null)
                {
                    this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Attempting to move {0} to another team/squad", capCommand.MatchedArguments[0].Argument), strSpeaker);

                    this.m_dicPlayers.QueueMovePlayer(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, moveDestination, response);
                }
            }
            catch (Exception e)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "1 " + e.Message);
            }
        }

        public void OnCommandForceMove(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            CPlayerSubset moveDestination = null;
            string response = "";

            if ((moveDestination = this.GetMoveDestination(strSpeaker, mtcCommand, capCommand, out response)) != null)
            {
                this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Attempting to move {0} to another team/squad", capCommand.MatchedArguments[0].Argument), strSpeaker);

                this.m_dicPlayers.ForceMovePlayer(capCommand.ResposeScope, strSpeaker, capCommand.MatchedArguments[0].Argument, moveDestination, response);
            }
        }

        private void m_dicPlayers_QueueResponse(string strScope, string strAccountName, string strMessage, string strTaskName, int iDelay, int iInterval, int iRepeat)
        {
            if (String.Compare(strScope, this.m_strPrivatePrefix) == 0)
            {
                this.ExecuteCommand("procon.protected.tasks.add", strTaskName, iDelay.ToString(), iInterval.ToString(), iRepeat.ToString(), "procon.protected.send", "admin.say", strMessage, "player", strAccountName);
            }
            else if (String.Compare(strScope, this.m_strPublicPrefix) == 0)
            {
                this.ExecuteCommand("procon.protected.tasks.add", strTaskName, iDelay.ToString(), iInterval.ToString(), iRepeat.ToString(), "procon.protected.send", "admin.say", strMessage, "all");
            }
            else if (String.Compare(strScope, this.m_strAdminsPrefix) == 0)
            {
                CPrivileges cpAccount = null;

                foreach (string soldierNames in this.m_dicPlayers.GetSoldierNameKeys())
                {
                    cpAccount = this.GetAccountPrivileges(soldierNames);

                    if (cpAccount != null && cpAccount.PrivilegesFlags > 0)
                    {
                        this.ExecuteCommand("procon.protected.tasks.add", strTaskName, iDelay.ToString(), iInterval.ToString(), iRepeat.ToString(), "procon.protected.send", "admin.say", strMessage, "player", soldierNames);
                    }
                }
            }
        }

        private void m_dicPlayers_SendYellingResponse(string strScope, string strAccountName, string strMessage)
        {
            if (String.Compare(strScope, this.m_strPrivatePrefix) == 0)
            {
                this.ExecuteCommand("procon.protected.send", "admin.yell", strMessage, this.m_strShowMessageLength, "player", strAccountName);
            }
            else if (String.Compare(strScope, this.m_strPublicPrefix) == 0)
            {
                this.ExecuteCommand("procon.protected.send", "admin.yell", strMessage, this.m_strShowMessageLength, "all");
            }
            else if (String.Compare(strScope, this.m_strAdminsPrefix) == 0)
            {
                CPrivileges cpAccount = null;

                foreach (string soldierNames in this.m_dicPlayers.GetSoldierNameKeys())
                {
                    cpAccount = this.GetAccountPrivileges(soldierNames);

                    if (cpAccount != null && cpAccount.PrivilegesFlags > 0)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.yell", strMessage, this.m_strShowMessageLength, "player", soldierNames);
                    }
                }
            }
        }

        private void m_dicPlayers_QueueYellingResponse(string strScope, string strAccountName, string strMessage, string strTaskName, int iDelay, int iInterval, int iRepeat)
        {
            if (String.Compare(strScope, this.m_strPrivatePrefix) == 0)
            {
                this.ExecuteCommand("procon.protected.tasks.add", strTaskName, iDelay.ToString(), iInterval.ToString(), iRepeat.ToString(), "procon.protected.send", "admin.yell", strMessage, this.m_strShowMessageLength, "player", strAccountName);
            }
            else if (String.Compare(strScope, this.m_strPublicPrefix) == 0)
            {
                this.ExecuteCommand("procon.protected.tasks.add", strTaskName, iDelay.ToString(), iInterval.ToString(), iRepeat.ToString(), "procon.protected.send", "admin.yell", strMessage, this.m_strShowMessageLength, "all");
            }
            else if (String.Compare(strScope, this.m_strAdminsPrefix) == 0)
            {
                CPrivileges cpAccount = null;

                foreach (string soldierNames in this.m_dicPlayers.GetSoldierNameKeys())
                {
                    cpAccount = this.GetAccountPrivileges(soldierNames);

                    if (cpAccount != null && cpAccount.PrivilegesFlags > 0)
                    {
                        this.ExecuteCommand("procon.protected.tasks.add", strTaskName, iDelay.ToString(), iInterval.ToString(), iRepeat.ToString(), "procon.protected.send", "admin.yell", strMessage, this.m_strShowMessageLength, "player", soldierNames);
                    }
                }
            }
        }

        private int GetDefaultSquadIDForMap()
        {
            int iDefaultSquadID = 0;

            if (this.m_currentMap != null)
            {
                iDefaultSquadID = this.m_currentMap.DefaultSquadID;
            }

            return iDefaultSquadID;
        }

        private int GetTeamsForMap()
        {
            int iTeamsCount = 0;

            if (this.m_currentMap != null)
            {
                iTeamsCount = this.m_currentMap.TeamNames.Count;
                if (this.m_strServerGameType == "bfbc2")
                {
                    iTeamsCount = this.GetTeamList("{TeamName}", this.m_currentMap.PlayList).Count;
                }
                else
                {
                    iTeamsCount = this.GetTeamListByPlayListForMap("{TeamName}", this.m_currentMap.FileName, this.m_currentMap.PlayList).Count;
                }
            }

            return iTeamsCount;
        }

        private bool TryGetTeamID(string formattedTeamName, out int iTeamID)
        {
            bool blValidTeamForMap = false;
            iTeamID = 0;
            CTeamName destinationTeam = null;

            if (this.m_currentMap != null)
            {
                if (this.m_strServerGameType == "bfbc2")
                {
                    destinationTeam = this.GetTeamNameByFormattedTeamName("{GameMode} {TeamName}", String.Format("{0} {1}", this.m_currentMap.GameMode, formattedTeamName));
                }
                else
                {
                    destinationTeam = this.GetTeamNameByFormattedTeamName("{GameMode} {FileName} {TeamName}", String.Format("{0} {1} {2}", this.m_currentMap.GameMode, this.m_currentMap.FileName, formattedTeamName));
                }
                if (destinationTeam != null)
                {
                    iTeamID = destinationTeam.TeamID;
                    blValidTeamForMap = true;
                }
            }

            return blValidTeamForMap;
        }

        private int GetSquadID(string strSquadName)
        {
            int iReturnSquadID = -1;

            for (int squadCount = 0; squadCount < this.m_squadNames.Count; squadCount++)
            {
                if (String.Compare(strSquadName, this.m_squadNames[squadCount], true) == 0)
                {
                    iReturnSquadID = squadCount;
                }
            }

            return iReturnSquadID;
        }

        private string CapatalizeFirstLetter(string strText)
        {
            return char.ToUpper(strText[0]) + strText.Substring(1).ToLower();
        }

        #region Events

        public override void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            this.m_dicPlayers.MoveQueuedPlayer(kKillerVictimDetails.Victim.SoldierName);
        }

        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.m_currentMap = this.GetMapByFilenamePlayList(csiServerInfo.Map, csiServerInfo.GameMode);

            this.RegisterAllCommands();
        }

        public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {
            this.m_currentMap = this.GetMapByFilename(mapFileName);

            this.RegisterAllCommands();
        }

        // Login events
        public override void OnLogin()
        {
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public override void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {
            this.m_dicPlayers.UpdatePlayerTeam(strSoldierName, iTeamID, iSquadID);
        }

        public override void OnPlayerSquadChange(string strSoldierName, int iTeamID, int iSquadID)
        {
            this.m_dicPlayers.UpdatePlayerTeam(strSoldierName, iTeamID, iSquadID);
        }

        public override void OnPlayerJoin(string strSoldierName)
        {
            this.m_dicPlayers.UpdatePlayer(strSoldierName, new CPlayerInfo(strSoldierName, "", 0, 24), null);

            this.RegisterAllCommands();
        }

        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            this.m_dicPlayers.RemovePlayer(playerInfo.SoldierName);

            this.RegisterAllCommands();
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {
            if (cpbiPlayer != null)
            {
                this.m_dicPlayers.UpdatePlayer(cpbiPlayer.SoldierName, null, cpbiPlayer);
            }
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    this.m_dicPlayers.UpdatePlayer(cpiPlayer.SoldierName, cpiPlayer, null);
                }

                // Remove stored players not on the server.
                foreach (PlayerInformation storedPlayer in this.m_dicPlayers)
                {
                    bool isWithinList = false;

                    foreach (CPlayerInfo playerInfo in lstPlayers)
                    {
                        if (String.Compare(storedPlayer.SoldierName, playerInfo.SoldierName) == 0)
                        {
                            isWithinList = true;
                            break;
                        }
                    }

                    if (isWithinList == false)
                    {
                        this.m_dicPlayers.RemovePlayer(storedPlayer.SoldierName);
                    }
                }

                this.RegisterAllCommands();
            }
        }

        #endregion

        #region internal player information

        internal enum BanTypes
        {
            None,
            FrostbiteName,
            FrostbiteEaGuid,
            PunkbusterGuid,
        }

        internal class PlayerInformationDictionary : KeyedCollection<string, PlayerInformation>
        {
            public delegate void SendResponseHandler(string strScope, string strAccountName, string strMessage);

            public event SendResponseHandler SendResponse;

            public delegate void ExecuteCommandHandler(params string[] words);

            public event ExecuteCommandHandler ExecuteCommand;

            public delegate void QueueYellingResponseHandler(string strScope, string strAccountName, string strMessage, string strTaskName, int iDelay, int iInterval, int iRepeat);

            public event QueueYellingResponseHandler QueueYellingResponse;

            public delegate void QueueResponseHandler(string strScope, string strAccountName, string strMessage, string strTaskName, int iDelay, int iInterval, int iRepeat);

            public event QueueResponseHandler QueueResponse;

            public delegate void SendYellingResponseHandler(string strScope, string strAccountName, string strMessage);

            public event SendYellingResponseHandler SendYellingResponse;

            private DateTime m_dtCountdownBlocker;

            private BanTypes m_banMethod;

            public BanTypes BanMethod
            {
                get
                {
                    return this.m_banMethod;
                }
                set
                {
                    this.m_banMethod = value;
                }
            }

            public enumBoolYesNo m_enBanIncAdmin;

            public string FrostbiteBanMethod
            {
                get
                {
                    string frostbiteBanMethod = "none";

                    if (this.BanMethod == BanTypes.FrostbiteName)
                    {
                        frostbiteBanMethod = "name";
                    }
                    else if (this.BanMethod == BanTypes.FrostbiteEaGuid)
                    {
                        frostbiteBanMethod = "guid";
                    }

                    return frostbiteBanMethod;
                }
            }

            public PlayerInformationDictionary()
            {
                this.BanMethod = BanTypes.None;
                this.m_enBanIncAdmin = enumBoolYesNo.No;
            }

            protected override string GetKeyForItem(PlayerInformation item)
            {
                return item.SoldierName;
            }

            public List<string> GetSoldierNameKeys()
            {
                List<string> soldierNames = new List<string>();

                foreach (PlayerInformation player in this)
                {
                    soldierNames.Add(player.SoldierName);
                }

                return soldierNames;
            }

            public void UpdatePlayer(string soldierName, CPlayerInfo vanillaInfo, CPunkbusterInfo punkbusterInfo)
            {
                if (this.Contains(soldierName) == true)
                {
                    if (vanillaInfo != null)
                    {
                        this[soldierName].VanillaInfo = vanillaInfo;
                    }

                    if (punkbusterInfo != null)
                    {
                        this[soldierName].PunkbusterInfo = punkbusterInfo;
                    }
                }
                else
                {
                    this.Add(new PlayerInformation(vanillaInfo, punkbusterInfo));
                }
            }

            public void UpdatePlayerTeam(string soldierName, int teamId, int squadId)
            {
                if (this.Contains(soldierName) == true && this[soldierName].VanillaInfo != null)
                {
                    this[soldierName].VanillaInfo.TeamID = teamId;
                    this[soldierName].VanillaInfo.SquadID = squadId;
                }
            }

            public void RemovePlayer(string soldierName)
            {
                if (String.Compare(soldierName, "Server", true) != 0 && this.Contains(soldierName) == true)
                {
                    this.Remove(soldierName);
                }
            }

            public void KickPlayer(string resposeScopeCapCommand, string accountNameStrSpeaker, string targetSoldierNameArgument, string reasonExtra)
            {
                if (this.Contains(targetSoldierNameArgument) == true)
                {
                    if (this[targetSoldierNameArgument].VanillaInfo != null)
                    {
                        this.SendResponse(resposeScopeCapCommand, accountNameStrSpeaker, "Kicking " + targetSoldierNameArgument + (reasonExtra.Length > 0 ? " for " + reasonExtra : ""));
                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", targetSoldierNameArgument, reasonExtra);
                    }
                    else if (this[targetSoldierNameArgument].PunkbusterInfo != null)
                    {
                        this.SendResponse(resposeScopeCapCommand, accountNameStrSpeaker, "Kicking " + targetSoldierNameArgument + (reasonExtra.Length > 0 ? " for " + reasonExtra : ""));
                        this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", String.Format("pb_sv_kick \"{0}\" 0 \"{1}\"", targetSoldierNameArgument, reasonExtra));
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Kick failed.", "player", accountNameStrSpeaker);
                    }
                }
            }

            public void KillPlayer(string responseScope, string accountName, string targetSoldierName, string reason)
            {
                if (this.Contains(targetSoldierName) == true)
                {
                    if (this[targetSoldierName].VanillaInfo != null)
                    {
                        this.SendResponse(responseScope, accountName, "Killing " + targetSoldierName + (reason.Length > 0 ? " for " + reason : ""));
                        this.ExecuteCommand("procon.protected.send", "admin.killPlayer", targetSoldierName);
                    }
                }
            }

            private string GetFrostbiteBanMethodTarget(CPlayerInfo player)
            {
                string target = "none";

                if (player != null)
                {
                    if (this.BanMethod == BanTypes.FrostbiteName)
                    {
                        target = player.SoldierName;
                    }
                    else if (this.BanMethod == BanTypes.FrostbiteEaGuid)
                    {
                        target = player.GUID;
                    }
                }

                return target;
            }

            public void TemporarilyBanPlayer(string responseScope, string accountName, string targetSoldierName, string reason, int time)
            {
                if (this.Contains(targetSoldierName) == true)
                {
                    if (this.BanMethod == BanTypes.PunkbusterGuid)
                    {
                        this.SendResponse(responseScope, accountName, "Temporarily banning " + targetSoldierName + " for " + time + " minutes" + (reason.Length > 0 ? ", reason: " + reason : ""));
                        // add admin name
                        if (this.m_enBanIncAdmin == enumBoolYesNo.Yes)
                        {
                            int iBanInfo = (80 - 5 - (accountName.Length + 3));
                            if (reason.Length > iBanInfo)
                            {
                                reason = reason.Substring(0, iBanInfo);
                            }
                            reason = reason + " (" + accountName + ")";
                        }
                        this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", String.Format("pb_sv_kick \"{0}\" {1} \"{2}\"", targetSoldierName, time.ToString(), "BC2! " + reason));
                    }
                    else
                    {
                        this.SendResponse(responseScope, accountName, "Temporarily banning " + targetSoldierName + " for " + time + " minutes" + (reason.Length > 0 ? ", reason: " + reason : ""));
                        // add admin name
                        if (this.m_enBanIncAdmin == enumBoolYesNo.Yes)
                        {
                            int iBanInfo = (80 - (accountName.Length + 3));
                            if (reason.Length > iBanInfo)
                            {
                                reason = reason.Substring(0, iBanInfo);
                            }
                            reason = reason + " (" + accountName + ")";
                        }
                        this.ExecuteCommand("procon.protected.send", "banList.add", this.FrostbiteBanMethod, this.GetFrostbiteBanMethodTarget(this[targetSoldierName].VanillaInfo), "seconds", (time * 60).ToString(), reason);
                        this.ExecuteCommand("procon.protected.send", "banList.save");
                        this.ExecuteCommand("procon.protected.send", "banList.list");
                    }
                }
            }

            public void PermanentBanPlayer(string responseScope, string accountName, string targetSoldierName, string reason)
            {
                if (this.Contains(targetSoldierName) == true)
                {
                    if (this.BanMethod == BanTypes.PunkbusterGuid)
                    {
                        this.SendResponse(responseScope, accountName, "Permanently banning " + targetSoldierName + (reason.Length > 0 ? ", reason: " + reason : ""));
                        // add admin name
                        if (this.m_enBanIncAdmin == enumBoolYesNo.Yes)
                        {
                            int iBanInfo = (80 - 5 - (accountName.Length + 3));
                            if (reason.Length > iBanInfo)
                            {
                                reason = reason.Substring(0, iBanInfo);
                            }
                            reason = reason + " (" + accountName + ")";
                        }
                        this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", String.Format("pb_sv_ban \"{0}\" \"{1}\"", targetSoldierName, "BC2! " + reason));
                    }
                    else
                    {
                        this.SendResponse(responseScope, accountName, "Permanently banning " + targetSoldierName + (reason.Length > 0 ? " for " + reason : ""));
                        // add admin name
                        if (this.m_enBanIncAdmin == enumBoolYesNo.Yes)
                        {
                            int iBanInfo = (80 - (accountName.Length + 3));
                            if (reason.Length > iBanInfo)
                            {
                                reason = reason.Substring(0, iBanInfo);
                            }
                            reason = reason + " (" + accountName + ")";
                        }
                        this.ExecuteCommand("procon.protected.send", "banList.add", this.FrostbiteBanMethod, this.GetFrostbiteBanMethodTarget(this[targetSoldierName].VanillaInfo), "perm", reason);
                        this.ExecuteCommand("procon.protected.send", "banList.save");
                        this.ExecuteCommand("procon.protected.send", "banList.list");
                    }
                }
            }

            public void QueueMovePlayer(string responseScope, string accountName, string targetSoldierName, CPlayerSubset newLocation, string adminMessage)
            {
                if (this.Contains(targetSoldierName) == true)
                {
                    this[targetSoldierName].MoveLocation = newLocation;

                    this.SendResponse(responseScope, accountName, adminMessage);
                }
            }

            public void ForceMovePlayer(string responseScope, string accountName, string targetSoldierName, CPlayerSubset newLocation, string adminMessage)
            {
                if (this.Contains(targetSoldierName) == true)
                {
                    this[targetSoldierName].MoveLocation = newLocation;

                    this.SendResponse(responseScope, accountName, adminMessage);

                    this.MoveQueuedPlayer(targetSoldierName);
                }
            }

            public void MoveQueuedPlayer(string targetSoldierName)
            {
                if (this.Contains(targetSoldierName) == true)
                {
                    if (this[targetSoldierName].MoveLocation != null)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "You have been moved to another team/squad by an admin.", "player", targetSoldierName);

                        // BFBC2 has a squad id parameter
                        this.ExecuteCommand("procon.protected.send", "admin.movePlayer", targetSoldierName, this[targetSoldierName].MoveLocation.TeamID.ToString(), this[targetSoldierName].MoveLocation.SquadID.ToString(), "true");
                        //this.ExecuteCommand("procon.protected.send", "admin.movePlayer", targetSoldierName, this[targetSoldierName].MoveLocation.TeamID.ToString(), this[targetSoldierName].MoveLocation.SquadID.ToString(), "true");

                        this[targetSoldierName].MoveLocation = null;
                    }
                }
            }

            public void Nuke(string responseScope, string accountName, CPlayerSubset target, string stringTarget)
            {
                this.SendResponse(responseScope, accountName, "Nuking " + stringTarget);

                // Kept it as a variable, may change command so they can specify a countdown..
                int iTimeout = 10;
                this.m_dtCountdownBlocker = DateTime.Now.AddSeconds((double)iTimeout);

                for (int i = 0; i < iTimeout; i++)
                {
                    if (i == iTimeout - 1)
                    {
                        this.QueueYellingResponse(responseScope, accountName, "INCOMING!!", "CInGameAdminNuke" + accountName, i, 1, 1);
                    }
                    else
                    {
                        this.QueueYellingResponse(responseScope, accountName, String.Format("NUKING {0} T-MINUS {1}", stringTarget, iTimeout - i), "CInGameAdminNuke" + accountName, i, 1, 1);
                    }
                }

                foreach (PlayerInformation player in this)
                {
                    if (player.VanillaInfo != null)
                    {
                        if (target.Subset == CPlayerSubset.PlayerSubsetType.All || (target.Subset == CPlayerSubset.PlayerSubsetType.Team && player.VanillaInfo.TeamID == target.TeamID))
                        {
                            this.ExecuteCommand("procon.protected.tasks.add", "CInGameAdminNuke" + accountName, iTimeout.ToString(), "1", "1", "procon.protected.send", "admin.killPlayer", player.SoldierName);
                        }
                    }
                }
            }

            public void CancelCountdowns(string responseScope, string accountName)
            {
                this.ExecuteCommand("procon.protected.tasks.remove", "CInGameAdminRestart" + accountName);
                this.ExecuteCommand("procon.protected.tasks.remove", "CInGameAdminForward" + accountName);
                this.ExecuteCommand("procon.protected.tasks.remove", "CInGameAdminEndRound" + accountName);
                this.ExecuteCommand("procon.protected.tasks.remove", "CInGameAdminNuke" + accountName);

                if (this.m_dtCountdownBlocker > DateTime.Now)
                {
                    this.SendYellingResponse(responseScope, accountName, "Canceled countdown");
                }

                this.m_dtCountdownBlocker = DateTime.Now;
            }

            public void PlayerSay(string responseScope, string accountName, string targetSoldierName, string message)
            {
                this.SendResponse(responseScope, accountName, "Saying \"" + message + "\" to " + targetSoldierName);
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "player", targetSoldierName);
            }

            public void PlayerYell(string responseScope, string accountName, string targetSoldierName, string message, string showMessageLength, bool m_blHasYellDuration)
            {
                this.SendResponse(responseScope, accountName, "Yelling \"" + message + "\" at " + targetSoldierName);
                if (m_blHasYellDuration == true)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.yell", message, showMessageLength, "player", targetSoldierName);
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.yell", message, "player", targetSoldierName);
                }
            }

            public void RestartLevel(string responseScope, string accountName, int time)
            {
                if (this.m_dtCountdownBlocker < DateTime.Now)
                {
                    if (time <= 60 && time >= 0)
                    {
                        this.m_dtCountdownBlocker = DateTime.Now.AddSeconds((double)time);

                        // I have no doubts over 60 seconds the timer won't be accurate but the end
                        // result will roughly be correct.
                        for (int i = 0; i < time; i++)
                        {
                            if (i == time - 1)
                            {
                                this.QueueYellingResponse(responseScope, accountName, "Restarting map..", "CInGameAdminRestart" + accountName, i, 1, 1);
                            }
                            else
                            {
                                this.QueueYellingResponse(responseScope, accountName, String.Format("Restarting map in {0} seconds", time - i), "CInGameAdminRestart" + accountName, i, 1, 1);
                            }
                        }

                        this.ExecuteCommand("procon.protected.tasks.add", "CInGameAdminRestart" + accountName, time.ToString(), "1", "1", "procon.protected.send", "admin.restartRound");
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Invalid arguments.  [timeout - value between 0 and 60 seconds]", "player", accountName);
                    }
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "A countdown is already in progress.", "player", accountName);
                }
            }

            public void RestartLevel(string responseScope, string accountName)
            {
                if (this.m_dtCountdownBlocker < DateTime.Now)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.restartRound");
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "A countdown is already in progress.", "player", accountName);
                }
            }

            public void ForwardLevel(string responseScope, string accountName, int time)
            {
                if (this.m_dtCountdownBlocker < DateTime.Now)
                {
                    if (time <= 60 && time >= 0)
                    {
                        this.m_dtCountdownBlocker = DateTime.Now.AddSeconds((double)time);

                        // I have no doubts over 60 seconds the timer won't be accurate but the end
                        // result will roughly be correct.
                        for (int i = 0; i < time; i++)
                        {
                            if (i == time - 1)
                            {
                                this.QueueYellingResponse(responseScope, accountName, "Forwarding to next level..", "CInGameAdminForward" + accountName, i, 1, 1);
                            }
                            else
                            {
                                this.QueueYellingResponse(responseScope, accountName, String.Format("Forwarding level in {0} seconds", time - i), "CInGameAdminForward" + accountName, i, 1, 1);
                            }
                        }

                        this.ExecuteCommand("procon.protected.tasks.add", "CInGameAdminForward" + accountName, time.ToString(), "1", "1", "procon.protected.send", "admin.runNextRound");
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Invalid arguments.  [timeout - value between 0 and 60 seconds]", "player", accountName);
                    }
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "A countdown is already in progress.", "player", accountName);
                }
            }

            public void ForwardLevel(string responseScope, string accountName)
            {
                if (this.m_dtCountdownBlocker < DateTime.Now)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.runNextRound");
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "A countdown is already in progress.", "player", accountName);
                }
            }

            public void EndLevel(string responseScope, string accountName, string teamID, int time)
            {
                if (this.m_dtCountdownBlocker < DateTime.Now)
                {
                    if (time <= 60 && time >= 0)
                    {
                        this.m_dtCountdownBlocker = DateTime.Now.AddSeconds((double)time);

                        // I have no doubts over 60 seconds the timer won't be accurate but the end
                        // result will roughly be correct.
                        for (int i = 0; i < time; i++)
                        {
                            if (i == time - 1)
                            {
                                this.QueueYellingResponse(responseScope, accountName, "Ending round..", "CInGameAdminEndRond" + accountName, i, 1, 1);
                            }
                            else
                            {
                                this.QueueYellingResponse(responseScope, accountName, String.Format("Ending round in {0} seconds", time - i), "CInGameAdminEndRound" + accountName, i, 1, 1);
                            }
                        }

                        this.ExecuteCommand("procon.protected.tasks.add", "CInGameAdminEndRound" + accountName, time.ToString(), "1", "1", "procon.protected.send", "mapList.endRound", teamID);
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Invalid arguments.  [timeout - value between 0 and 60 seconds]", "player", accountName);
                    }
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "A countdown is already in progress.", "player", accountName);
                }
            }

            public void EndLevel(string responseScope, string accountName, string teamID)
            {
                if (this.m_dtCountdownBlocker < DateTime.Now)
                {
                    this.ExecuteCommand("procon.protected.send", "mapList.endRound", teamID);
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "A countdown is already in progress.", "player", accountName);
                }
            }
        }

        internal class PlayerInformation
        {
            private CPlayerInfo m_vanillaInfo;

            public CPlayerInfo VanillaInfo
            {
                get
                {
                    return this.m_vanillaInfo;
                }
                set
                {
                    this.m_vanillaInfo = value;
                }
            }

            private CPunkbusterInfo m_punkbusterInfo;

            public CPunkbusterInfo PunkbusterInfo
            {
                get
                {
                    return this.m_punkbusterInfo;
                }
                set
                {
                    this.m_punkbusterInfo = value;
                }
            }

            private CPlayerSubset m_moveLocation;

            public CPlayerSubset MoveLocation
            {
                get
                {
                    return this.m_moveLocation;
                }
                set
                {
                    this.m_moveLocation = value;
                }
            }

            public string SoldierName
            {
                get
                {
                    string soldierName = String.Empty;

                    if (this.VanillaInfo != null)
                    {
                        soldierName = this.VanillaInfo.SoldierName;
                    }
                    else if (this.PunkbusterInfo != null)
                    {
                        soldierName = this.PunkbusterInfo.SoldierName;
                    }

                    return soldierName;
                }
            }

            public PlayerInformation(CPlayerInfo vanillaInfo, CPunkbusterInfo punkbusterInfo)
            {
                this.VanillaInfo = vanillaInfo;
                this.PunkbusterInfo = punkbusterInfo;
            }
        }

        #endregion
    }
}