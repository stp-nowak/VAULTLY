import madge from 'madge';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { madgeConfig } from './madge.config.ts';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const entryPath = path.resolve(__dirname, '../index.ts');
const reportFileName = path.resolve(__dirname, 'circularDependencies.json');

const circularDependenciesReport = async () => {
  const response = await madge(entryPath, madgeConfig);

  const circularDependencies = response.circular();
  const dependenciesCount = circularDependencies.length;
  const result = {
    count: dependenciesCount,
    circularDependencies,
  };

  fs.writeFile(reportFileName, JSON.stringify(result), (error) => {
    if (error) {
      console.log("Couldn't generate circular dependency report", error);
    }
  });
};

circularDependenciesReport();
