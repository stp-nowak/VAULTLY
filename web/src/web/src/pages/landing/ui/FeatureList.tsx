import { Eyebrow } from '@vaultly/ui';

const features = [
  {
    body: 'Every account and its balance side by side, cash included, so you always know what you have.',
    title: 'Accounts',
  },
  {
    body: 'One feed of everything you spend and earn, sorted into categories you can change.',
    title: 'Transactions',
  },
  {
    body: 'A monthly limit for each category, and a clear view of how much of it is left.',
    title: 'Budgets',
  },
  {
    body: 'Money put aside for what matters to you, with the progress always in sight.',
    title: 'Goals',
  },
];

/** What the app covers, as a numbered list. Target of the hero's "See what it does" link. */
export function FeatureList() {
  return (
    <section
      aria-labelledby="features-heading"
      className="mx-auto grid max-w-300 scroll-mt-24 gap-10 px-6 py-20 lg:grid-cols-[minmax(0,4fr)_minmax(0,8fr)] lg:gap-16 lg:px-8"
      id="features"
    >
      <div className="grid content-start gap-4 lg:sticky lg:top-28 lg:self-start">
        <Eyebrow>What it does</Eyebrow>
        <h2 className="text-[clamp(2rem,4vw,3.25rem)] leading-tight" id="features-heading">
          Four views of the same money.
        </h2>
      </div>

      <ol className="border-t border-line">
        {features.map((feature, index) => (
          <li className="grid grid-cols-[3.5rem_minmax(0,1fr)] gap-x-6 border-b border-line py-7" key={feature.title}>
            <span aria-hidden className="font-display text-2xl text-accent tabular-nums">
              {String(index + 1).padStart(2, '0')}
            </span>
            <div className="grid gap-2">
              <h3 className="font-display text-2xl font-semibold tracking-display">{feature.title}</h3>
              <p className="max-w-[38rem] text-muted">{feature.body}</p>
            </div>
          </li>
        ))}
      </ol>
    </section>
  );
}
