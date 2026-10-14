using Godot;
namespace IndexCities.Game;

public partial class Main
{
    async void FinancesSmoke()
    {
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/simulation-perf/finances-"+Guid.NewGuid().ToString("N")));
        try {
            Require(DisplayServer.GetName()!="headless","Finanças exigem interface renderizada");
            Require(_sim.Population>0&&_sim.State.CityHallPlaced,"Cenário usa cidade povoada real");
            _speed=0;_autosaveMinutes=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"saves");
            var before=_sim.SaveToJson();var cash=_sim.CityBalance;var global=_sim.State.Wallets["global"];var money=_sim.State.Wallets.Values.Sum();var loanCount=_sim.State.Loans.Count;
            ShowFinances();await RecoveryFrame();var window=_modal!;
            var editor=(SpinBox)window.FindChild("FinanceLoanAmount",true,false);var scroll=RecoveryNodes<ScrollContainer>(window).Single();
            var submit=RecoveryNodes<Button>(window).Single(b=>b.Text=="Contratar empréstimo");
            var contracts=(VBoxContainer)window.FindChild("FinanceContracts",true,false);var flows=(GridContainer)window.FindChild("FinanceFlows",true,false);
            void CheckBalances() {
                Require(((Label)window.FindChild("FinanceAvailable",true,false)).Text==Cash(_sim.Available),"Disponível mostra o saldo real");
                Require(((Label)window.FindChild("FinanceCommitted",true,false)).Text==Cash(_sim.Committed),"Reservado mostra os compromissos reais");
                Require(((Label)window.FindChild("FinanceTotal",true,false)).Text==Cash(_sim.CityBalance),"Caixa total mostra o saldo real");
            }
            Require(_sim.SaveToJson()==before,"Abrir finanças não altera a cidade");
            await RecoveryCapture(System.IO.Path.Combine(directory,"finances-open.png"),GetViewport());
            editor.Value=10000;editor.GetLineEdit().GrabFocus();await RecoveryFrame();var focus=window.GuiGetFocusOwner();var offset=scroll.ScrollVertical;
            Require(focus==editor.GetLineEdit(),"Editor de valor recebeu foco");
            submit.EmitSignal(Godot.Button.SignalName.Pressed);await RecoveryFrame();CheckBalances();
            Require(_sim.CityBalance==cash+10000&&_sim.State.Wallets["global"]==global-10000&&_sim.State.Wallets.Values.Sum()==money,"Empréstimo transfere exatamente dinheiro existente");
            Require(_sim.State.Loans.Count==loanCount+1&&contracts.GetChildCount()==_sim.State.Loans.Count(l=>l.Principal>0),"Contrato novo aparece no mesmo modal");
            Require(RecoveryNodes<Label>(flows).Any(l=>l.Text=="Empréstimo"),"Fluxos incluem a transação real");
            Require(_modal==window&&editor.Value==10000&&window.GuiGetFocusOwner()==focus&&scroll.ScrollVertical==offset,"Ação preserva janela, campo, foco e rolagem");
            await RecoveryCapture(System.IO.Path.Combine(directory,"finances-loan.png"),GetViewport());

            // A second real command outside the button exercises the periodic
            // refresh. It transfers existing money and creates an actual loan.
            Require(_sim.Borrow(1).Success,"Comando real para verificar atualização periódica");
            _sim.Advance(2);await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);CheckBalances();
            Require(contracts.GetChildCount()==_sim.State.Loans.Count(l=>l.Principal>0),"Timer atualiza contratos sem callback do botão");
            Require(_sim.State.Wallets.Values.Sum()==money,"Simulação e atualização preservam oferta monetária");
            editor.Value=(double)_sim.State.Tuning.LoanLimit;var rejected=_sim.SaveToJson();
            submit.EmitSignal(Godot.Button.SignalName.Pressed);await RecoveryFrame();CheckBalances();
            Require(_sim.SaveToJson()==rejected,"Recusar valor acima do principal permitido não altera estado");
            Require(editor.Value==(double)_sim.State.Tuning.LoanLimit&&_modal==window&&scroll.ScrollVertical==offset,"Recusa preserva edição e modal");
            await RecoveryCapture(System.IO.Path.Combine(directory,"finances-rejected.png"),GetViewport());
            Require(TrySaveCity(),"Cópia de teste salva no diretório isolado");
            var saved=CitySaves.Load(_cityId);var restored=IndexCities.Simulation.CitySimulation.FromJson(saved.CityJson);
            Require(restored.SaveToJson()==_sim.SaveToJson(),"Save/load mantém empréstimos, recursos e progresso");
            CloseModal();await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);
            Require(_modal==null&&_sim.SaveToJson()==rejected,"Fechar o painel cancela atualização sem mudar a partida");
            GD.Print("INDEXCITIES_FINANCES_OK contracts=live flows=live rejection=preserved money="+money+" save-load=identical directory="+directory);GetTree().Quit();
        } catch(Exception exception) { GD.PushError(exception.ToString());GetTree().Quit(1); }
        finally { CitySaves.VerificationDirectory=null; }
    }
}
