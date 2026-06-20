import type { CatalogEntry, StrategyResponse } from "./strategies";

export const IS_DEMO_MODE = import.meta.env.VITE_DEMO_MODE === "true";

// ── Reference data ────────────────────────────────────────────────────────────
// Used by fetchReferenceData() to populate the SearchPage list.

export const MOCK_REFERENCE_DATA = {
  regions: [
    { id: "r1", name: "Київська область", nameFull: "Київська область" },
    { id: "r2", name: "Львівська область", nameFull: "Львівська область" },
  ],
  districts: [
    {
      id: "d1",
      name: "Броварський район",
      nameFull: "Броварський район",
      regionId: "r1",
    },
    {
      id: "d2",
      name: "Яворівський район",
      nameFull: "Яворівський район",
      regionId: "r2",
    },
  ],
  communities: [
    {
      id: "c1",
      name: "Броварська міська громада",
      nameFull: "Броварська міська територіальна громада",
      regionId: "r1",
      districtId: "d1",
    },
    {
      id: "c2",
      name: "Яворівська міська громада",
      nameFull: "Яворівська міська територіальна громада",
      regionId: "r2",
      districtId: "d2",
    },
    {
      id: "c3",
      name: "Бориспільська міська громада",
      nameFull: "Бориспільська міська територіальна громада",
      regionId: "r1",
      districtId: "d1",
    },
  ],
  strategies: [
    {
      id: "s1",
      title: "Стратегія розвитку Київської області 2021–2027",
      regionId: "r1",
      districtId: null,
      communityId: null,
    },
    {
      id: "s2",
      title: "Стратегія розвитку Броварського району 2022–2026",
      regionId: "r1",
      districtId: "d1",
      communityId: null,
    },
    {
      id: "s3",
      title: "Програма розвитку Броварської громади 2023–2027",
      regionId: "r1",
      districtId: "d1",
      communityId: "c1",
    },
    {
      id: "s4",
      title: "Стратегія розвитку Яворівської громади 2024–2028",
      regionId: "r2",
      districtId: "d2",
      communityId: "c2",
    },
  ],
} as const;

// ── Strategy detail fixtures ───────────────────────────────────────────────────
// Used by getCatalogEntryById() and loadStrategyForCatalogEntry().

const CATALOG_ENTRIES: Record<string, CatalogEntry> = {
  s1: {
    id: "s1",
    city: "Київська область",
    unitId: "r1",
    strategyId: "s1",
    title: "Стратегія розвитку Київської області 2021–2027",
    period: "2021–2027",
    summary: "Стратегічний документ: Київська область.",
    directions: [
      "Підвищення якості освіти",
      "Розвиток охорони здоров'я",
      "Залучення інвестицій",
      "Підтримка малого та середнього бізнесу",
      "Розвиток транспортної інфраструктури",
    ],
    status: "active",
    strategyUrl: null,
    officialSourceUrl: null,
    fileUrl: null,
  },
  s2: {
    id: "s2",
    city: "Броварський район",
    unitId: "d1",
    strategyId: "s2",
    title: "Стратегія розвитку Броварського району 2022–2026",
    period: "2022–2026",
    summary: "Стратегічний документ: Броварський район.",
    directions: [
      "Розвиток агропромислового комплексу",
      "Екологічна безпека",
      "Цифровізація послуг",
    ],
    status: "active",
    strategyUrl: null,
    officialSourceUrl: null,
    fileUrl: null,
  },
  s3: {
    id: "s3",
    city: "Бровари",
    unitId: "c1",
    strategyId: "s3",
    title: "Програма розвитку Броварської громади 2023–2027",
    period: "2023–2027",
    summary: "Стратегічний документ: Броварська міська територіальна громада.",
    directions: [
      "Комунальна інфраструктура",
      "Молодіжна політика",
      "Спорт та дозвілля",
    ],
    status: "active",
    strategyUrl: null,
    officialSourceUrl: null,
    fileUrl: null,
  },
  s4: {
    id: "s4",
    city: "Яворів",
    unitId: "c2",
    strategyId: "s4",
    title: "Стратегія розвитку Яворівської громади 2024–2028",
    period: "2024–2028",
    summary:
      "Стратегічний документ: Яворівська міська територіальна громада.",
    directions: [
      "Туристичний розвиток",
      "Підтримка підприємництва",
      "Культура та освіта",
    ],
    status: "active",
    strategyUrl: null,
    officialSourceUrl: null,
    fileUrl: null,
  },
};

const STRATEGY_RESPONSES: Record<string, StrategyResponse> = {
  s1: {
    unit: { id: "r1", name: "Київська область", type: "Region" },
    strategy: {
      id: "s1",
      title: "Стратегія розвитку Київської області 2021–2027",
      regionId: "r1",
      districtId: null,
      communityId: null,
      strategicGoals: [
        {
          id: "s1-sg1",
          label: "1",
          number: 1,
          title: "Розвиток людського капіталу",
          operationalGoals: [
            {
              id: "s1-og1",
              label: "1.1",
              number: "1.1",
              title: "Підвищення якості освіти",
              programTasks: [
                {
                  id: "s1-t1",
                  label: "1.1.1",
                  description: "Модернізація шкільної інфраструктури",
                },
                {
                  id: "s1-t2",
                  label: "1.1.2",
                  description: "Впровадження цифрових освітніх технологій",
                },
                {
                  id: "s1-t3",
                  label: "1.1.3",
                  description: "Підготовка та підвищення кваліфікації педагогів",
                },
              ],
            },
            {
              id: "s1-og2",
              label: "1.2",
              number: "1.2",
              title: "Розвиток охорони здоров'я",
              programTasks: [
                {
                  id: "s1-t4",
                  label: "1.2.1",
                  description: "Оновлення медичного обладнання у лікарнях",
                },
                {
                  id: "s1-t5",
                  label: "1.2.2",
                  description:
                    "Розширення мережі первинної медичної допомоги",
                },
              ],
            },
          ],
        },
        {
          id: "s1-sg2",
          label: "2",
          number: 2,
          title: "Економічний розвиток та інвестиції",
          operationalGoals: [
            {
              id: "s1-og3",
              label: "2.1",
              number: "2.1",
              title: "Залучення інвестицій",
              programTasks: [
                {
                  id: "s1-t6",
                  label: "2.1.1",
                  description: "Розробка інвестиційного паспорту регіону",
                },
                {
                  id: "s1-t7",
                  label: "2.1.2",
                  description: "Створення індустріальних парків",
                },
              ],
            },
            {
              id: "s1-og4",
              label: "2.2",
              number: "2.2",
              title: "Підтримка малого та середнього бізнесу",
              programTasks: [
                {
                  id: "s1-t8",
                  label: "2.2.1",
                  description: "Програма мікрокредитування підприємців",
                },
                {
                  id: "s1-t9",
                  label: "2.2.2",
                  description: "Консультаційні центри підтримки бізнесу",
                },
                {
                  id: "s1-t10",
                  label: "2.2.3",
                  description: "Цифрова трансформація малих підприємств",
                },
              ],
            },
          ],
        },
        {
          id: "s1-sg3",
          label: "3",
          number: 3,
          title: "Інфраструктура та екологія",
          operationalGoals: [
            {
              id: "s1-og5",
              label: "3.1",
              number: "3.1",
              title: "Розвиток транспортної інфраструктури",
              programTasks: [
                {
                  id: "s1-t11",
                  label: "3.1.1",
                  description:
                    "Ремонт та будівництво доріг регіонального значення",
                },
                {
                  id: "s1-t12",
                  label: "3.1.2",
                  description: "Розвиток громадського транспорту",
                },
              ],
            },
          ],
        },
      ],
    },
  },
  s2: {
    unit: { id: "d1", name: "Броварський район", type: "District" },
    strategy: {
      id: "s2",
      title: "Стратегія розвитку Броварського району 2022–2026",
      regionId: "r1",
      districtId: "d1",
      communityId: null,
      strategicGoals: [
        {
          id: "s2-sg1",
          label: "1",
          number: 1,
          title: "Розвиток агропромислового комплексу",
          operationalGoals: [
            {
              id: "s2-og1",
              label: "1.1",
              number: "1.1",
              title: "Підтримка фермерських господарств",
              programTasks: [
                {
                  id: "s2-t1",
                  label: "1.1.1",
                  description: "Програми субсидування аграріїв",
                },
                {
                  id: "s2-t2",
                  label: "1.1.2",
                  description: "Розвиток кооперативного руху",
                },
              ],
            },
          ],
        },
        {
          id: "s2-sg2",
          label: "2",
          number: 2,
          title: "Екологічна безпека та сталий розвиток",
          operationalGoals: [
            {
              id: "s2-og2",
              label: "2.1",
              number: "2.1",
              title: "Екологічна безпека",
              programTasks: [
                {
                  id: "s2-t3",
                  label: "2.1.1",
                  description: "Рекультивація забруднених ділянок",
                },
                {
                  id: "s2-t4",
                  label: "2.1.2",
                  description: "Впровадження роздільного збору відходів",
                },
              ],
            },
            {
              id: "s2-og3",
              label: "2.2",
              number: "2.2",
              title: "Цифровізація адміністративних послуг",
              programTasks: [
                {
                  id: "s2-t5",
                  label: "2.2.1",
                  description: "Е-документообіг у районних органах влади",
                },
              ],
            },
          ],
        },
      ],
    },
  },
  s3: {
    unit: {
      id: "c1",
      name: "Броварська міська територіальна громада",
      type: "Community",
    },
    strategy: {
      id: "s3",
      title: "Програма розвитку Броварської громади 2023–2027",
      regionId: "r1",
      districtId: "d1",
      communityId: "c1",
      strategicGoals: [
        {
          id: "s3-sg1",
          label: "1",
          number: 1,
          title: "Комфортне середовище для мешканців",
          operationalGoals: [
            {
              id: "s3-og1",
              label: "1.1",
              number: "1.1",
              title: "Комунальна інфраструктура",
              programTasks: [
                {
                  id: "s3-t1",
                  label: "1.1.1",
                  description: "Реконструкція водопровідних мереж",
                },
                {
                  id: "s3-t2",
                  label: "1.1.2",
                  description: "Освітлення вулиць та пішохідних зон",
                },
              ],
            },
            {
              id: "s3-og2",
              label: "1.2",
              number: "1.2",
              title: "Спорт та дозвілля",
              programTasks: [
                {
                  id: "s3-t3",
                  label: "1.2.1",
                  description: "Будівництво спортивних майданчиків",
                },
                {
                  id: "s3-t4",
                  label: "1.2.2",
                  description: "Реновація міського парку",
                },
              ],
            },
          ],
        },
        {
          id: "s3-sg2",
          label: "2",
          number: 2,
          title: "Молодіжна та соціальна політика",
          operationalGoals: [
            {
              id: "s3-og3",
              label: "2.1",
              number: "2.1",
              title: "Молодіжна політика",
              programTasks: [
                {
                  id: "s3-t5",
                  label: "2.1.1",
                  description: "Молодіжний центр та коворкінг",
                },
                {
                  id: "s3-t6",
                  label: "2.1.2",
                  description: "Програми зайнятості молоді",
                },
              ],
            },
          ],
        },
      ],
    },
  },
  s4: {
    unit: {
      id: "c2",
      name: "Яворівська міська територіальна громада",
      type: "Community",
    },
    strategy: {
      id: "s4",
      title: "Стратегія розвитку Яворівської громади 2024–2028",
      regionId: "r2",
      districtId: "d2",
      communityId: "c2",
      strategicGoals: [
        {
          id: "s4-sg1",
          label: "1",
          number: 1,
          title: "Туризм та культурна спадщина",
          operationalGoals: [
            {
              id: "s4-og1",
              label: "1.1",
              number: "1.1",
              title: "Туристичний розвиток",
              programTasks: [
                {
                  id: "s4-t1",
                  label: "1.1.1",
                  description: "Розробка туристичних маршрутів",
                },
                {
                  id: "s4-t2",
                  label: "1.1.2",
                  description: "Реставрація пам'яток архітектури",
                },
                {
                  id: "s4-t3",
                  label: "1.1.3",
                  description: "Промоція громади на туристичних платформах",
                },
              ],
            },
            {
              id: "s4-og2",
              label: "1.2",
              number: "1.2",
              title: "Культура та освіта",
              programTasks: [
                {
                  id: "s4-t4",
                  label: "1.2.1",
                  description: "Реконструкція будинку культури",
                },
                {
                  id: "s4-t5",
                  label: "1.2.2",
                  description: "Цифровізація бібліотечних фондів",
                },
              ],
            },
          ],
        },
        {
          id: "s4-sg2",
          label: "2",
          number: 2,
          title: "Економічний розвиток громади",
          operationalGoals: [
            {
              id: "s4-og3",
              label: "2.1",
              number: "2.1",
              title: "Підтримка підприємництва",
              programTasks: [
                {
                  id: "s4-t6",
                  label: "2.1.1",
                  description: "Бізнес-інкубатор для стартапів",
                },
                {
                  id: "s4-t7",
                  label: "2.1.2",
                  description: "Ярмарки місцевих виробників",
                },
              ],
            },
          ],
        },
      ],
    },
  },
};

export function getMockCatalogEntry(id: string): CatalogEntry | null {
  return CATALOG_ENTRIES[id] ?? null;
}

export function getMockStrategyResponse(id: string): StrategyResponse | null {
  return STRATEGY_RESPONSES[id] ?? null;
}
