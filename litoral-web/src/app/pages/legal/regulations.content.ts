import { LegalDocument, LegalLanguage } from './legal.models';

export const REGULATIONS: Record<LegalLanguage, LegalDocument> = {
  en: {
    title: 'Regulations',
    subtitle: 'The rules every Litoral Linhas Aéreas pilot agrees to when creating an account.',
    updatedLabel: 'Last updated',
    sections: [
      {
        id: 'general',
        title: '1. General',
        paragraphs: [
          'Litoral Linhas Aéreas (ICAO LTL, callsign LITORAL) is a virtual airline for flight simulation. It is a non-commercial hobby project and has no connection with any real airline.'
        ],
        items: [
          'Flights are operated on the VATSIM and IVAO networks. Other networks may be added in the future.',
          'The Litoral fleet is 100% Boeing 737 (737-600, -700, -800, -800BCF, -900ER and MAX 8).',
          'Only quality Boeing 737 add-ons are accepted, such as PMDG, iFly and Zibo/LevelUp. The default aircraft that come with the simulator are not accepted.',
          'Supported simulators: MSFS 2020, MSFS 2024, X-Plane 11/12 and Prepar3D.'
        ]
      },
      {
        id: 'membership',
        title: '2. Membership',
        items: [
          'You must be at least 16 years old to join. Accounts created with false information are deleted.',
          'Registration is free and there is no entrance exam.',
          'Every new pilot starts as First Officer (FO) at the base chosen during registration.',
          'You may be a member of other virtual airlines, but the same flight can never be logged in Litoral and in another virtual airline.',
          'Your account is personal and non-transferable. Do not share your login with anyone.'
        ]
      },
      {
        id: 'conduct',
        title: '3. Conduct',
        items: [
          'Treat every pilot, controller and staff member with respect, on the site, on Discord and on the networks.',
          'Follow the rules of the network you are flying on (VATSIM Code of Conduct or IVAO Regulations).',
          'It is not allowed to use the Litoral name, logo or liveries in a way that suggests you speak for the airline without staff authorization.',
          'Crew documents and staff material are for members only and must not be shared outside the airline.',
          'Trying to access staff areas without permission, exploiting bugs or manipulating flights, points or hours leads to the deletion of the account.'
        ]
      },
      {
        id: 'activity',
        title: '4. Activity',
        paragraphs: ['Fly at least once every 40 days. Inactivity is counted from your last flight report (or from the day you joined, or from your last reactivation):'],
        table: {
          head: ['Days without flying', 'What happens'],
          rows: [
            ['30', 'You receive a warning notification.'],
            ['41 to 89', 'You lose 50 points per day until your next flight.'],
            ['90', 'Your account is deactivated.']
          ]
        },
        note: 'A deactivated account can not book flights. To come back, contact the staff and ask for your account to be reactivated. Staff members are not affected by these rules.'
      },
      {
        id: 'bookings',
        title: '5. Bookings',
        items: [
          'Every flight starts at your current location, with an aircraft that is parked at the departure airport.',
          'You can only have one open booking at a time.',
          'A booking expires after 24 hours if the flight is not started. Special flights expire at the end of their window.',
          'Custom flights to destinations that are not a base cost 50 points. Jumpseats to airports that are not a base cost 3 points per nautical mile.',
          'Cancelling or not finishing a special flight costs 500 points.',
          'Each airport has a minimum rank. You can only operate at airports that your rank allows.'
        ]
      },
      {
        id: 'flights',
        title: '6. Flights and flight reports (PIREPs)',
        items: [
          'Flights are tracked by the Litoral ACARS and sent as a flight report (PIREP) at the end of the flight. If the ACARS fails, you may send a manual PIREP with a comment explaining what happened.',
          'The aircraft type, registration and route flown must match your booking.',
          'Flights must be flown online on VATSIM or IVAO, with at least 80% of the flight connected. Offline flights are rejected.',
          'Your flight plan remarks must include OPR/LITORAL PIC/ followed by your name.',
          'Use a comment in the PIREP to explain anything unusual (diversion, go-around, technical problem). A comment helps the review but does not guarantee approval.',
          'It is not allowed to fly on two networks at the same time, to log the same flight in another virtual airline or to use another flight tracker for Litoral flights.'
        ]
      },
      {
        id: 'review',
        title: '7. Review and penalties',
        paragraphs: [
          'Every PIREP is scored automatically by the Litoral scoring rules, which you can read on the Points page. ACARS PIREPs with a conformity of 98% or more and no rejection items are approved automatically. All other PIREPs, including every manual PIREP, are reviewed by the Safety team.',
          'A rejected PIREP gives no points, but the flight hours still count. Rejections in the same month add up:'
        ],
        table: {
          head: ['Rejection in the month', 'Penalty'],
          rows: [
            ['1st', '−1,000 points'],
            ['2nd', '−1,500 points'],
            ['3rd', '−2,000 points'],
            ['4th', '−10,000 points and the account is deactivated']
          ]
        },
        note: 'Your points balance can become negative. The values above are the current ones and may be changed by the Safety team.'
      },
      {
        id: 'career',
        title: '8. Career',
        table: {
          head: ['Rank', 'Requirements'],
          rows: [
            ['FO · First Officer', 'Every pilot who joins Litoral'],
            ['CMD · Captain', '500 flight hours and 30,000 points']
          ]
        },
        items: [
          'Promotions are automatic as soon as you meet the requirements.',
          'A Captain whose balance falls below 30,000 points goes back to First Officer automatically, and is moved to the base if standing at a Captain-only airport.',
          'Every month, the top 3 pilots in flight hours and in number of flights earn a bonus of 800, 600 and 400 points.'
        ]
      },
      {
        id: 'account',
        title: '9. Your account',
        items: [
          'You can delete your account at any time in Settings. The deletion is permanent and removes your profile, flights, hours, points and login.',
          'The number of a deleted account becomes available to new pilots. The number of a deactivated account stays reserved for it.',
          'You can download a copy of your data and make privacy requests in Settings.',
          'How your data is handled is described in the Privacy Policy.'
        ]
      },
      {
        id: 'final',
        title: '10. Final provisions',
        items: [
          'By creating an account you confirm that you have read and agree to these Regulations and to the Privacy Policy.',
          'These Regulations may change at any time. Changes are announced through NOTAMs and site notifications, apply from the date they are published, and must be accepted the next time you sign in.',
          'Cases not covered here are decided by the staff.',
          'Breaking these rules may lead to warnings, penalties, deactivation or the deletion of the account, depending on how serious it is.'
        ]
      }
    ]
  },
  pt: {
    title: 'Regulamento',
    subtitle: 'As regras que todo piloto da Litoral Linhas Aéreas aceita ao criar a conta.',
    updatedLabel: 'Última atualização',
    sections: [
      {
        id: 'general',
        title: '1. Disposições gerais',
        paragraphs: [
          'A Litoral Linhas Aéreas (ICAO LTL, callsign LITORAL) é uma companhia aérea virtual para simulação de voo. É um projeto sem fins comerciais e não tem vínculo com nenhuma companhia aérea real.'
        ],
        items: [
          'Os voos são realizados nas networks VATSIM e IVAO. Outras networks podem ser adicionadas no futuro.',
          'A frota da Litoral é 100% Boeing 737 (737-600, -700, -800, -800BCF, -900ER e MAX 8).',
          'São aceitos apenas add-ons de qualidade do Boeing 737, como PMDG, iFly e Zibo/LevelUp. As aeronaves padrão que vêm com o simulador não são aceitas.',
          'Simuladores suportados: MSFS 2020, MSFS 2024, X-Plane 11/12 e Prepar3D.'
        ]
      },
      {
        id: 'membership',
        title: '2. Cadastro',
        items: [
          'É preciso ter no mínimo 16 anos para entrar. Contas criadas com informações falsas são excluídas.',
          'O cadastro é gratuito e não há prova de admissão.',
          'Todo piloto novo começa como First Officer (FO) na base escolhida no cadastro.',
          'Você pode fazer parte de outras companhias virtuais, mas o mesmo voo nunca pode ser registrado na Litoral e em outra companhia virtual.',
          'A conta é pessoal e intransferível. Não compartilhe o seu login com ninguém.'
        ]
      },
      {
        id: 'conduct',
        title: '3. Conduta',
        items: [
          'Trate todos os pilotos, controladores e membros do staff com respeito, no site, no Discord e nas networks.',
          'Siga as regras da network em que estiver voando (Code of Conduct da VATSIM ou regulamento da IVAO).',
          'Não é permitido usar o nome, a logo ou as pinturas da Litoral de forma que pareça que você fala pela companhia sem autorização do staff.',
          'Os documentos da tripulação e o material do staff são exclusivos dos membros e não podem ser divulgados fora da companhia.',
          'Tentar acessar áreas do staff sem permissão, explorar falhas ou manipular voos, pontos ou horas leva à exclusão da conta.'
        ]
      },
      {
        id: 'activity',
        title: '4. Atividade',
        paragraphs: ['Voe pelo menos uma vez a cada 40 dias. A inatividade é contada a partir do seu último PIREP (ou do dia do cadastro, ou da última reativação):'],
        table: {
          head: ['Dias sem voar', 'O que acontece'],
          rows: [
            ['30', 'Você recebe uma notificação de aviso.'],
            ['41 a 89', 'Você perde 50 pontos por dia até o próximo voo.'],
            ['90', 'A conta é desativada.']
          ]
        },
        note: 'Uma conta desativada não pode reservar voos. Para voltar, fale com o staff e peça a reativação da conta. Membros do staff não são afetados por essas regras.'
      },
      {
        id: 'bookings',
        title: '5. Reservas',
        items: [
          'Todo voo começa na sua localização atual, com uma aeronave que esteja no aeroporto de partida.',
          'Você só pode ter uma reserva aberta por vez.',
          'A reserva expira em 24 horas se o voo não for iniciado. As Special Flights expiram no fim da janela.',
          'Voos personalizados para destinos que não são base custam 50 pontos. O jumpseat para aeroportos que não são base custa 3 pontos por milha náutica.',
          'Cancelar ou não concluir uma Special Flight custa 500 pontos.',
          'Cada aeroporto tem uma patente mínima. Você só pode operar nos aeroportos que a sua patente permite.'
        ]
      },
      {
        id: 'flights',
        title: '6. Voos e PIREPs',
        items: [
          'Os voos são acompanhados pelo ACARS da Litoral e enviados como PIREP ao final do voo. Se o ACARS falhar, você pode enviar um PIREP manual com um comentário explicando o que aconteceu.',
          'O tipo de aeronave, a matrícula e a rota voada precisam ser os da sua reserva.',
          'Os voos devem ser feitos online na VATSIM ou na IVAO, com pelo menos 80% do voo conectado. Voos offline são reprovados.',
          'Os remarks do plano de voo devem ter OPR/LITORAL PIC/ seguido do seu nome.',
          'Use o comentário do PIREP para explicar qualquer situação fora do normal (alternado, arremetida, pane). O comentário ajuda na revisão, mas não garante a aprovação.',
          'Não é permitido voar em duas networks ao mesmo tempo, registrar o mesmo voo em outra companhia virtual ou usar outro rastreador de voo nos voos da Litoral.'
        ]
      },
      {
        id: 'review',
        title: '7. Revisão e penalidades',
        paragraphs: [
          'Todo PIREP é pontuado automaticamente pelas regras de pontuação da Litoral, que podem ser lidas na página Points. PIREPs do ACARS com conformidade de 98% ou mais e sem itens de reprovação são aprovados automaticamente. Todos os outros, incluindo todo PIREP manual, são revisados pelo time de Safety.',
          'Um PIREP rejeitado não dá pontos, mas as horas de voo continuam valendo. As rejeições no mesmo mês se acumulam:'
        ],
        table: {
          head: ['Rejeição no mês', 'Penalidade'],
          rows: [
            ['1ª', '−1.000 pontos'],
            ['2ª', '−1.500 pontos'],
            ['3ª', '−2.000 pontos'],
            ['4ª', '−10.000 pontos e a conta é desativada']
          ]
        },
        note: 'O saldo de pontos pode ficar negativo. Os valores acima são os atuais e podem ser alterados pelo time de Safety.'
      },
      {
        id: 'career',
        title: '8. Carreira',
        table: {
          head: ['Patente', 'Requisitos'],
          rows: [
            ['FO · First Officer', 'Todo piloto que entra na Litoral'],
            ['CMD · Captain', '500 horas de voo e 30.000 pontos']
          ]
        },
        items: [
          'A promoção é automática assim que você cumpre os requisitos.',
          'Um comandante cujo saldo fique abaixo de 30.000 pontos volta a ser First Officer automaticamente e, se estiver num aeroporto só de comandantes, é movido para a base.',
          'Todo mês, os 3 pilotos com mais horas de voo e os 3 com mais voos ganham um bônus de 800, 600 e 400 pontos.'
        ]
      },
      {
        id: 'account',
        title: '9. A sua conta',
        items: [
          'Você pode excluir a sua conta a qualquer momento em Settings. A exclusão é permanente e apaga perfil, voos, horas, pontos e login.',
          'O número de uma conta excluída fica disponível para novos pilotos. O número de uma conta desativada continua reservado para ela.',
          'Você pode baixar uma cópia dos seus dados e fazer pedidos de privacidade em Settings.',
          'Como os seus dados são tratados está descrito na Política de Privacidade.'
        ]
      },
      {
        id: 'final',
        title: '10. Disposições finais',
        items: [
          'Ao criar a conta, você confirma que leu e concorda com este Regulamento e com a Política de Privacidade.',
          'Este Regulamento pode mudar a qualquer momento. As mudanças são avisadas por NOTAM e pelas notificações do site, valem a partir da data de publicação e precisam ser aceitas na próxima vez que você entrar.',
          'Os casos que não estiverem previstos aqui são decididos pelo staff.',
          'Descumprir estas regras pode levar a advertência, penalidade, desativação ou exclusão da conta, conforme a gravidade.'
        ]
      }
    ]
  }
};