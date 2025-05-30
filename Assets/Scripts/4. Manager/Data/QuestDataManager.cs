using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class QuestDataManager : DataHandler<QuestData>
{
    private UGSDataHandler<GameData.Quest> questTable;


    protected override void Init()
    {
        if (questTable != null)
            return; 

        questTable = new UGSDataHandler<GameData.Quest>();

        List<GameData.Quest> quests = questTable.GetAll();
        foreach (GameData.Quest quest in quests)
        {
            QuestData questData = new QuestData(quest); 
            datas.Add(quest.index, questData);
            dataList.Add(questData);
        }
    }
}
