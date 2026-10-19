using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CafeLife {
 public sealed class CafeCasino:MonoBehaviour {
  public readonly List<int> player=new List<int>(),dealer=new List<int>();readonly List<int> deck=new List<int>();
  CafeGame Game=>GetComponent<CafeGame>();public bool Active=>Game.blackjackActive;public string Result{get;private set;}="Bahsini seç, kartlarını aç.";public bool Revealed=>!Active;
  public static int Score(IEnumerable<int> cards){int sum=0,aces=0;foreach(int card in cards){int rank=card%13+1;if(rank==1){sum+=11;aces++;}else sum+=Mathf.Min(rank,10);}while(sum>21&&aces-->0)sum-=10;return sum;}
  public static string Label(int card){int rank=card%13+1;return (rank==1?"A":rank==11?"J":rank==12?"Q":rank==13?"K":rank.ToString())+"\n"+new[]{"♠","♥","♦","♣"}[card/13];}
  void Update(){if(!Game.Paused||!Game.ui||!Game.ui.IsPage("casino")||Keyboard.current==null)return;var k=Keyboard.current;if(Active){if(k.hKey.wasPressedThisFrame)Hit();else if(k.sKey.wasPressedThisFrame)Stand();}else{if(k.digit1Key.wasPressedThisFrame)StartHand(10);else if(k.digit2Key.wasPressedThisFrame)StartHand(50);else if(k.digit3Key.wasPressedThisFrame)StartHand(200);}}
int Draw(){int card=deck[deck.Count-1];deck.RemoveAt(deck.Count-1);return card;}
  void Sync(){Game.blackjackPlayer=Score(player);Game.blackjackDealer=Active?Score(dealer.Take(1)):Score(dealer);Game.blackjackMessage=Result;}
  public void StartHand(int bet){if(Active)return;if(bet<=0||bet>10000||Game.data.money<bet){Result="Bu bahis için yeterli coin yok.";Refresh();return;}deck.Clear();for(int i=0;i<52;i++)deck.Add(i);for(int i=51;i>0;i--){int j=Random.Range(0,i+1);int temp=deck[i];deck[i]=deck[j];deck[j]=temp;}player.Clear();dealer.Clear();Game.blackjackBet=bet;Game.data.money-=bet;Game.expansion.Expense(bet);Game.blackjackActive=true;player.Add(Draw());dealer.Add(Draw());player.Add(Draw());dealer.Add(Draw());Result="Kart çek veya elindeki sayıda dur.";if(Score(player)==21||Score(dealer)==21)Resolve();else Refresh();}
  public void Hit(){if(!Active)return;player.Add(Draw());if(Score(player)>21){Game.blackjackActive=false;Result="21'i aştın · -"+Game.blackjackBet+" coin";Refresh();}else if(Score(player)==21)Stand();else Refresh();}
  public void Stand(){if(!Active)return;while(Score(dealer)<17)dealer.Add(Draw());Resolve();}
  void Resolve(){int p=Score(player),d=Score(dealer),payout=0;bool natural=player.Count==2&&p==21,dealerNatural=dealer.Count==2&&d==21;Game.blackjackActive=false;if(p<=21&&(!dealerNatural||natural)&&(d>21||p>d||natural&&!dealerNatural)){payout=natural?Game.blackjackBet*5/2:Game.blackjackBet*2;Result=(natural?"BLACKJACK!":"Kazandın!")+" Net +"+(payout-Game.blackjackBet)+" coin";}else if(p==d&&natural==dealerNatural){payout=Game.blackjackBet;Result="Berabere · bahis iade edildi.";}else Result="Krupiye kazandı · -"+Game.blackjackBet+" coin";Game.data.money+=payout;Game.expansion.Data.dayIncome+=payout;Refresh();}
  void Refresh(){Sync();Game.Save();var table=GetComponent<CafeTableBlackjack>();if(table&&table.Opened)table.Refresh();}
 }
 public sealed class CafeStreetActivities:MonoBehaviour {
  public int shots,hits;public bool playing;public string message="5 atış. Gösterge ortadayken at!";CafeGame Game=>GetComponent<CafeGame>();public float Aim=>Mathf.PingPong(Time.unscaledTime*.85f,1);
  public void Begin(){shots=hits=0;playing=true;message="Yeşil bölgeyi yakala.";Game.ui.ShowMenu("arcade");}
  public void Shoot()=>ShootAt(Aim);
  public void ShootAt(float aim){if(!playing)return;shots++;bool hit=Mathf.Abs(aim-.5f)<=.11f;if(hit)hits++;message=hit?"İsabet!":"Çemberden döndü.";if(shots>=5){playing=false;var d=Game.data.hundred;d.basketBest=Mathf.Max(d.basketBest,hits);message="Tur bitti · "+hits+" / 5 isabet";if(hits>=3&&d.streetRewardDay!=Game.data.day){d.streetRewardDay=Game.data.day;Game.data.money+=20;Game.expansion.Data.dayIncome+=20;Game.data.reputation=Mathf.Min(100,Game.data.reputation+2);message+=" · +20 coin / +2 itibar";}else message+=" · Günlük ödül: 3 isabet";Game.Save();}Game.ui.ShowMenu("arcade");}
  public static int CafeScore(CafeGame g)=>Mathf.RoundToInt(g.data.reputation*10+g.data.served*8+g.data.decor*40+g.data.machine*35+(g.data.hundred.qualityCount>0?g.data.hundred.qualitySum/(float)g.data.hundred.qualityCount*4:0));
 }
}
