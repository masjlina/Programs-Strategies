import CollapsibleBlock from '../CollapsibleBlock/CollapsibleBlock.jsx'

export function StrategyGoalsTree({ strategy }) {
  return (
    <section className="goals-tree">
      <h2 className="goals-tree__title">Структура стратегічних цілей</h2>

      {strategy.strategic_goals.map((strategicGoal) => (
        <CollapsibleBlock
          key={strategicGoal.id}
          title={`Стратегічна ціль ${strategicGoal.label}. ${strategicGoal.title}`}
          defaultOpen={false}
        >
          {strategicGoal.operational_goals.map((operationalGoal) => (
            <div key={operationalGoal.id} className="goals-tree__og">
              <CollapsibleBlock
                title={`Оперативна ціль ${operationalGoal.label}. ${operationalGoal.title}`}
              >
                {operationalGoal.tasks.map((task) => (
                  <div key={task.id} className="goals-tree__task">
                    <CollapsibleBlock title={`Завдання ${task.label}`}>
                      <p>{task.description}</p>
                    </CollapsibleBlock>
                  </div>
                ))}
              </CollapsibleBlock>
            </div>
          ))}
        </CollapsibleBlock>
      ))}
    </section>
  )
}
