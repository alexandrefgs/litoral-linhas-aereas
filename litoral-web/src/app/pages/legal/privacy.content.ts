import { LEGAL_CONTACT_EMAIL, LegalDocument, LegalLanguage } from './legal.models';

export const PRIVACY: Record<LegalLanguage, LegalDocument> = {
  en: {
    title: 'Privacy Policy',
    subtitle: 'What data Litoral Linhas Aéreas collects, why, and what you can do about it.',
    updatedLabel: 'Last updated',
    sections: [
      {
        id: 'about',
        title: '1. About this policy',
        paragraphs: [
          'Litoral Linhas Aéreas is a non-commercial virtual airline for flight simulation, run by volunteers. This policy explains how we handle the personal data of the pilots who use this site.',
          'We follow the Brazilian General Data Protection Law (LGPD, Law No. 13,709/2018). Litoral Linhas Aéreas is the controller of the data described here.'
        ]
      },
      {
        id: 'collect',
        title: '2. Data we collect',
        table: {
          head: ['Type', 'Data', 'When'],
          rows: [
            ['Account', 'First and last name, e-mail, password (stored only as a secure hash) and base', 'When you register'],
            ['Profile (optional)', 'Date of birth, country, state and city, VATSIM, IVAO and Discord IDs, SimBrief username, simulator aircraft, profile photo and cover', 'When you fill in Settings'],
            ['Flights', 'Bookings, flight reports (PIREPs), aircraft position and track during the flight, simulator events, scores, hours and points', 'When you book and fly'],
            ['Security', 'IP address, date and time of logins, failed logins and important actions', 'When you sign in and use the site']
          ]
        },
        note: 'We do not collect documents, payment data or any sensitive data.'
      },
      {
        id: 'use',
        title: '3. How we use your data',
        items: [
          'To create and keep your pilot account and identify you on the site.',
          'To run the airline: bookings, flight tracking, scoring, career, rankings, statistics and the live map.',
          'To send you notifications about your flights, points and account.',
          'To keep the site secure, investigate abuse and keep a record of important changes (audit logs).'
        ],
        note: 'The legal bases are the performance of the membership you accept when registering (LGPD art. 7, V), our legitimate interest in keeping the site secure (art. 7, IX) and your consent for the optional profile data (art. 7, I), which you can remove at any time.'
      },
      {
        id: 'visibility',
        title: '4. What other pilots can see',
        items: [
          'Your profile is visible to signed-in pilots: your name in the format you choose in Settings, pilot ID, rank, photo, cover, statistics, flights and flight map, and your age.',
          'Your e-mail, date of birth and Discord are shown only to you and to the staff.',
          'Your flights in progress appear on the live map with your name, aircraft and position.'
        ]
      },
      {
        id: 'sharing',
        title: '5. Sharing',
        paragraphs: ['We do not sell, rent or trade your data. To work, the site uses these services:'],
        table: {
          head: ['Service', 'What it receives'],
          rows: [
            ['SimBrief (Navigraph)', 'Your SimBrief username, to load your flight plan (OFP)'],
            ['VATSIM and IVAO', 'Nothing is sent by us. The site only links to their flight plan pages and public trackers, using your flight data'],
            ['CARTO and flagcdn.com', 'Map images and country flags are loaded by your browser, so these services see your IP address'],
            ['aviationweather.gov', 'Only airport codes, to fetch METAR and TAF'],
            ['Hosting provider', 'Stores the site and the database on our behalf']
          ]
        },
        note: 'Data may also be disclosed when required by law or by a court order.'
      },
      {
        id: 'storage',
        title: '6. Browser storage',
        items: [
          'The site uses the browser local storage only for your login session and your theme preference (light, dark or system).',
          'We do not use advertising or tracking cookies, and we do not use analytics tools that follow you across other sites.'
        ]
      },
      {
        id: 'retention',
        title: '7. How long we keep your data',
        items: [
          'Your data is kept while your account exists, including when it is deactivated.',
          'When you delete your account in Settings (or when the staff deletes it), your profile, photos, flights, PIREPs, hours, points, bookings, notifications, login and e-mail are permanently deleted.',
          'Audit logs are kept to protect the airline and its members. They store the name, pilot ID and e-mail related to the action, and stay after the account is deleted.',
          'Hours and cycles flown stay in the aircraft history, without your name.'
        ]
      },
      {
        id: 'rights',
        title: '8. Your rights',
        paragraphs: ['Under the LGPD (art. 18), you can at any time:'],
        items: [
          'Confirm that we process your data and access it.',
          'Correct incomplete, wrong or outdated data. Most of it can be edited directly in Settings.',
          'Ask for the deletion of your data, which you can do yourself by deleting your account.',
          'Ask for a copy of your data in a readable format.',
          'Withdraw your consent for the optional profile data.',
          'Ask which services your data is shared with.'
        ],
        note: `To exercise these rights, contact us at ${LEGAL_CONTACT_EMAIL}. You can also file a complaint with the ANPD (Brazilian Data Protection Authority).`
      },
      {
        id: 'security',
        title: '9. Security',
        items: [
          'Passwords are stored only as a secure hash. Nobody, including the staff, can see your password.',
          'The site uses HTTPS, staff access is limited to what each position needs, and important actions are recorded in audit logs.',
          'No system is 100% secure. Use a strong password that you do not use on other sites and never share it.'
        ]
      },
      {
        id: 'minors',
        title: '10. Minors',
        paragraphs: ['Litoral only accepts pilots who are 16 or older. Accounts created by younger users, or with a false age, are deleted.']
      },
      {
        id: 'communications',
        title: '11. Communications',
        paragraphs: ['We contact you through the site notifications and NOTAMs. In the future we may also send e-mails about your account, flights and important changes. We do not send advertising.']
      },
      {
        id: 'changes',
        title: '12. Changes and contact',
        items: [
          'This policy may be updated. The date at the top shows the last change, and important changes are announced on the site.',
          `Questions about privacy: ${LEGAL_CONTACT_EMAIL}.`
        ]
      }
    ]
  },
  pt: {
    title: 'Política de Privacidade',
    subtitle: 'Quais dados a Litoral Linhas Aéreas coleta, por que, e o que você pode fazer a respeito.',
    updatedLabel: 'Última atualização',
    sections: [
      {
        id: 'about',
        title: '1. Sobre esta política',
        paragraphs: [
          'A Litoral Linhas Aéreas é uma companhia aérea virtual sem fins comerciais para simulação de voo, mantida por voluntários. Esta política explica como tratamos os dados pessoais dos pilotos que usam este site.',
          'Seguimos a Lei Geral de Proteção de Dados (LGPD, Lei nº 13.709/2018). A Litoral Linhas Aéreas é a controladora dos dados descritos aqui.'
        ]
      },
      {
        id: 'collect',
        title: '2. Dados que coletamos',
        table: {
          head: ['Tipo', 'Dados', 'Quando'],
          rows: [
            ['Conta', 'Nome e sobrenome, e-mail, senha (guardada apenas como hash seguro) e base', 'No cadastro'],
            ['Perfil (opcional)', 'Data de nascimento, país, estado e cidade, IDs da VATSIM, IVAO e Discord, usuário do SimBrief, aeronaves do simulador, foto de perfil e capa', 'Quando você preenche o Settings'],
            ['Voos', 'Reservas, PIREPs, posição e trilha da aeronave durante o voo, eventos do simulador, pontuação, horas e pontos', 'Quando você reserva e voa'],
            ['Segurança', 'Endereço IP, data e hora dos logins, tentativas de login com erro e ações importantes', 'Quando você entra e usa o site']
          ]
        },
        note: 'Não coletamos documentos, dados de pagamento nem dados sensíveis.'
      },
      {
        id: 'use',
        title: '3. Como usamos os seus dados',
        items: [
          'Para criar e manter a sua conta de piloto e identificar você no site.',
          'Para operar a companhia: reservas, acompanhamento dos voos, pontuação, carreira, rankings, estatísticas e mapa ao vivo.',
          'Para enviar notificações sobre os seus voos, pontos e conta.',
          'Para manter o site seguro, investigar abusos e registrar as alterações importantes (logs de auditoria).'
        ],
        note: 'As bases legais são a execução da participação que você aceita no cadastro (LGPD art. 7º, V), o nosso legítimo interesse em manter o site seguro (art. 7º, IX) e o seu consentimento para os dados opcionais do perfil (art. 7º, I), que você pode remover a qualquer momento.'
      },
      {
        id: 'visibility',
        title: '4. O que outros pilotos podem ver',
        items: [
          'O seu perfil é visível para os pilotos logados: o seu nome no formato que você escolher em Settings, ID de piloto, patente, foto, capa, estatísticas, voos e mapa de voos, e a sua idade.',
          'O seu e-mail, data de nascimento e Discord aparecem apenas para você e para o staff.',
          'Os seus voos em andamento aparecem no mapa ao vivo com o seu nome, aeronave e posição.'
        ]
      },
      {
        id: 'sharing',
        title: '5. Compartilhamento',
        paragraphs: ['Não vendemos, alugamos nem trocamos os seus dados. Para funcionar, o site usa estes serviços:'],
        table: {
          head: ['Serviço', 'O que recebe'],
          rows: [
            ['SimBrief (Navigraph)', 'O seu usuário do SimBrief, para carregar o plano de voo (OFP)'],
            ['VATSIM e IVAO', 'Nada é enviado por nós. O site só tem links para as páginas de plano de voo e os rastreadores públicos deles, usando os dados do seu voo'],
            ['CARTO e flagcdn.com', 'As imagens do mapa e as bandeiras dos países são carregadas pelo seu navegador, então esses serviços veem o seu endereço IP'],
            ['aviationweather.gov', 'Apenas os códigos dos aeroportos, para buscar METAR e TAF'],
            ['Provedor de hospedagem', 'Guarda o site e o banco de dados em nosso nome']
          ]
        },
        note: 'Os dados também podem ser fornecidos quando houver obrigação legal ou ordem judicial.'
      },
      {
        id: 'storage',
        title: '6. Armazenamento no navegador',
        items: [
          'O site usa o armazenamento local do navegador apenas para a sua sessão de login e para o tema escolhido (claro, escuro ou do sistema).',
          'Não usamos cookies de publicidade ou rastreamento, nem ferramentas de análise que acompanham você em outros sites.'
        ]
      },
      {
        id: 'retention',
        title: '7. Por quanto tempo guardamos os dados',
        items: [
          'Os seus dados são mantidos enquanto a conta existir, inclusive quando ela estiver desativada.',
          'Quando você exclui a conta em Settings (ou quando o staff a exclui), o seu perfil, fotos, voos, PIREPs, horas, pontos, reservas, notificações, login e e-mail são apagados de forma permanente.',
          'Os logs de auditoria são mantidos para proteger a companhia e os seus membros. Eles guardam o nome, o ID de piloto e o e-mail ligados à ação, e continuam existindo depois da exclusão da conta.',
          'As horas e ciclos voados continuam no histórico da aeronave, sem o seu nome.'
        ]
      },
      {
        id: 'rights',
        title: '8. Os seus direitos',
        paragraphs: ['Pela LGPD (art. 18), você pode a qualquer momento:'],
        items: [
          'Confirmar que tratamos os seus dados e ter acesso a eles.',
          'Corrigir dados incompletos, errados ou desatualizados. A maior parte pode ser editada direto em Settings.',
          'Pedir a exclusão dos seus dados, o que você mesmo pode fazer excluindo a conta.',
          'Pedir uma cópia dos seus dados em formato legível.',
          'Revogar o consentimento dos dados opcionais do perfil.',
          'Saber com quais serviços os seus dados são compartilhados.'
        ],
        note: `Para exercer esses direitos, fale com a gente em ${LEGAL_CONTACT_EMAIL}. Você também pode reclamar na ANPD (Autoridade Nacional de Proteção de Dados).`
      },
      {
        id: 'security',
        title: '9. Segurança',
        items: [
          'As senhas são guardadas apenas como hash seguro. Ninguém, nem o staff, consegue ver a sua senha.',
          'O site usa HTTPS, o acesso do staff é limitado ao que cada posição precisa, e as ações importantes ficam registradas em logs de auditoria.',
          'Nenhum sistema é 100% seguro. Use uma senha forte, que você não use em outros sites, e nunca a compartilhe.'
        ]
      },
      {
        id: 'minors',
        title: '10. Menores de idade',
        paragraphs: ['A Litoral só aceita pilotos com 16 anos ou mais. Contas criadas por menores dessa idade, ou com idade falsa, são excluídas.']
      },
      {
        id: 'communications',
        title: '11. Comunicações',
        paragraphs: ['Falamos com você pelas notificações do site e pelos NOTAMs. No futuro também poderemos enviar e-mails sobre a sua conta, voos e mudanças importantes. Não enviamos publicidade.']
      },
      {
        id: 'changes',
        title: '12. Alterações e contato',
        items: [
          'Esta política pode ser atualizada. A data no topo mostra a última alteração, e as mudanças importantes são avisadas no site.',
          `Dúvidas sobre privacidade: ${LEGAL_CONTACT_EMAIL}.`
        ]
      }
    ]
  }
};